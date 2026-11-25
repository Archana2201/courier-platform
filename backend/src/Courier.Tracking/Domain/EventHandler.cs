using System.Text.Json;
using Courier.BuildingBlocks.Events;
using Courier.Tracking.Data;
using Microsoft.EntityFrameworkCore;

namespace Courier.Tracking.Domain;

/// <summary>Turns incoming shipment integration events into customer-facing tracking rows (idempotently).</summary>
public class TrackingEventHandler
{
    private readonly TrackingDbContext _db;

    public TrackingEventHandler(TrackingDbContext db) => _db = db;

    /// <returns>true if the event was applied, false if it was a duplicate or an unknown type.</returns>
    public async Task<bool> HandleAsync(EventEnvelope envelope, CancellationToken ct = default)
    {
        if (await _db.TrackingEvents.AnyAsync(t => t.SourceEventId == envelope.EventId, ct))
            return false;

        TrackingEvent? row = envelope.Type switch
        {
            EventTypes.ShipmentCreated => FromCreated(envelope),
            EventTypes.ShipmentStatusChanged => FromStatusChanged(envelope),
            _ => null,
        };
        if (row is null) return false;

        _db.TrackingEvents.Add(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static TrackingEvent FromCreated(EventEnvelope envelope)
    {
        var e = JsonSerializer.Deserialize<ShipmentCreated>(envelope.Payload)!;
        return new TrackingEvent
        {
            TrackingNumber = e.TrackingNumber,
            Status = "Created",
            Location = e.Origin,
            Description = $"Shipment booked from {e.Origin} to {e.Destination}",
            OccurredAtUtc = e.AtUtc,
            SourceEventId = envelope.EventId,
        };
    }

    private static TrackingEvent FromStatusChanged(EventEnvelope envelope)
    {
        var e = JsonSerializer.Deserialize<ShipmentStatusChanged>(envelope.Payload)!;
        return new TrackingEvent
        {
            TrackingNumber = e.TrackingNumber,
            Status = e.Status,
            Location = e.Location,
            Description = Describe(e.Status),
            OccurredAtUtc = e.AtUtc,
            SourceEventId = envelope.EventId,
        };
    }

    public static string Describe(string status) => status switch
    {
        "PickedUp" => "Parcel picked up by courier",
        "InTransit" => "Parcel in transit",
        "OutForDelivery" => "Out for delivery",
        "Delivered" => "Delivered",
        "Cancelled" => "Shipment cancelled",
        _ => status,
    };
}
