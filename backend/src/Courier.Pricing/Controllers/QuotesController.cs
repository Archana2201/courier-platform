using Courier.Pricing.Data;
using Courier.Pricing.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Courier.Pricing.Controllers;

[ApiController]
[Route("api/quotes")]
public class QuotesController : ControllerBase
{
    private readonly PricingDbContext _db;
    private readonly QuoteCalculator _calculator;

    public QuotesController(PricingDbContext db, QuoteCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    [HttpPost]
    public async Task<ActionResult<QuoteResult>> Create([FromBody] QuoteRequest request, CancellationToken ct)
    {
        var error = Validate(request);
        if (error is not null) return BadRequest(new { error });

        var rateCard = await _db.RateCards.AsNoTracking().FirstOrDefaultAsync(r => r.ServiceLevel == request.ServiceLevel, ct);
        if (rateCard is null) return BadRequest(new { error = $"Unknown service level '{request.ServiceLevel}'." });

        return Ok(_calculator.Calculate(request, rateCard));
    }

    [HttpGet("rate-cards")]
    public async Task<IEnumerable<RateCard>> RateCards(CancellationToken ct) =>
        await _db.RateCards.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);

    public static string? Validate(QuoteRequest r)
    {
        if (r.WeightKg <= 0 || r.WeightKg > QuoteCalculator.MaxWeightKg)
            return $"Weight must be between 0 and {QuoteCalculator.MaxWeightKg} kg.";
        if (r.LengthCm <= 0 || r.WidthCm <= 0 || r.HeightCm <= 0)
            return "Dimensions must be greater than zero.";
        if (string.IsNullOrWhiteSpace(r.OriginPostcode) || string.IsNullOrWhiteSpace(r.DestinationPostcode))
            return "Origin and destination postcodes are required.";
        return null;
    }
}
