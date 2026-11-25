using Courier.BuildingBlocks.Events;
using Courier.BuildingBlocks.Outbox;
using Courier.Shipments.Clients;
using Courier.Shipments.Data;
using Courier.Shipments.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Courier.Shipments.Controllers;

public record CreateShipmentRequest(Address Sender, Address Recipient, decimal WeightKg,
    decimal LengthCm, decimal WidthCm, decimal HeightCm, string ServiceLevel);

public record UpdateStatusRequest(string Status, string? Location);

[ApiController]
[Route("api/shipments")]
public class ShipmentsController : ControllerBase
{
    private readonly ShipmentsDbContext _db;
    private readonly IPricingClient _pricing;
    private readonly ShipmentFactory _factory;

    public ShipmentsController(ShipmentsDbContext db, IPricingClient pricing, ShipmentFactory factory)
    {
        _db = db;
        _pricing = pricing;
        _factory = factory;
    }

    [HttpGet]
    public async Task<IEnumerable<Shipment>> List(CancellationToken ct)
    {
        var all = await _db.Shipments.AsNoTracking().Include(s => s.History).ToListAsync(ct);
        return all.OrderByDescending(s => s.CreatedAtUtc).Take(50);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Shipment>> Get(Guid id, CancellationToken ct)
    {
        var shipment = await _db.Shipments.AsNoTracking().Include(s => s.History).FirstOrDefaultAsync(s => s.Id == id, ct);
        return shipment is null ? NotFound() : shipment;
    }

    [HttpPost]
    public async Task<ActionResult<Shipment>> Create([FromBody] CreateShipmentRequest request, CancellationToken ct)
    {
        PricingResponse quote;
        try
        {
            quote = await _pricing.GetQuoteAsync(new PricingRequest(request.WeightKg, request.LengthCm, request.WidthCm,
                request.HeightCm, request.ServiceLevel, request.Sender.Postcode, request.Recipient.Postcode), ct);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = $"Pricing service rejected or failed the quote: {ex.Message}" });
        }

        Shipment shipment;
        try
        {
            shipment = _factory.Create(new CreateShipmentCommand(request.Sender, request.Recipient, request.WeightKg,
                request.LengthCm, request.WidthCm, request.HeightCm, request.ServiceLevel), quote.Total, quote.Currency, DateTime.UtcNow);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        // Business change and integration event are saved in ONE transaction (Outbox pattern).
        _db.Shipments.Add(shipment);
        _db.Outbox.Add(OutboxMessage.From(EventTypes.ShipmentCreated, new ShipmentCreated(
            shipment.Id, shipment.TrackingNumber, shipment.Sender.City, shipment.Recipient.City, shipment.CreatedAtUtc)));
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = shipment.Id }, shipment);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<Shipment>> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ShipmentStatus>(request.Status, ignoreCase: true, out var next))
            return BadRequest(new { error = $"Unknown status '{request.Status}'." });

        var shipment = await _db.Shipments.Include(s => s.History).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null) return NotFound();

        try
        {
            var change = shipment.MoveTo(next, request.Location, DateTime.UtcNow);
            _db.StatusChanges.Add(change);
            _db.Outbox.Add(OutboxMessage.From(EventTypes.ShipmentStatusChanged, new ShipmentStatusChanged(
                shipment.Id, shipment.TrackingNumber, next.ToString(), request.Location, change.AtUtc)));
            await _db.SaveChangesAsync(ct);
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }

        return shipment;
    }
}
