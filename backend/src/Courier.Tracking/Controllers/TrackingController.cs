using Courier.BuildingBlocks.Events;
using Courier.Tracking.Data;
using Courier.Tracking.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Courier.Tracking.Controllers;

[ApiController]
public class TrackingController : ControllerBase
{
    private readonly TrackingDbContext _db;
    private readonly TrackingEventHandler _handler;

    public TrackingController(TrackingDbContext db, TrackingEventHandler handler)
    {
        _db = db;
        _handler = handler;
    }

    /// <summary>Public lookup: full event timeline for a tracking number.</summary>
    [HttpGet("api/tracking/{trackingNumber}")]
    public async Task<ActionResult<object>> Get(string trackingNumber, CancellationToken ct)
    {
        var number = trackingNumber.Trim().ToUpperInvariant();
        var events = (await _db.TrackingEvents.AsNoTracking().Where(t => t.TrackingNumber == number).ToListAsync(ct))
            .OrderBy(t => t.OccurredAtUtc)
            .ToList();
        if (events.Count == 0) return NotFound(new { error = $"No tracking information for {number}." });

        return new
        {
            trackingNumber = number,
            currentStatus = events[^1].Status,
            events = events.Select(e => new { e.Status, e.Location, e.Description, e.OccurredAtUtc }),
        };
    }

    /// <summary>Internal endpoint called by the event bus (HTTP transport).</summary>
    [HttpPost("api/events")]
    public async Task<IActionResult> Receive([FromBody] EventEnvelope envelope, CancellationToken ct)
    {
        var applied = await _handler.HandleAsync(envelope, ct);
        return Ok(new { applied });
    }
}
