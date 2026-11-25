namespace Courier.BuildingBlocks.Events;

/// <summary>Transport wrapper for an integration event (what travels between services).</summary>
public record EventEnvelope(Guid EventId, string Type, DateTime OccurredAtUtc, string Payload);

/// <summary>Raised by Shipments when a new shipment has been booked.</summary>
public record ShipmentCreated(Guid ShipmentId, string TrackingNumber, string Origin, string Destination, DateTime AtUtc);

/// <summary>Raised by Shipments whenever a shipment moves to a new status.</summary>
public record ShipmentStatusChanged(Guid ShipmentId, string TrackingNumber, string Status, string? Location, DateTime AtUtc);

public static class EventTypes
{
    public const string ShipmentCreated = nameof(ShipmentCreated);
    public const string ShipmentStatusChanged = nameof(ShipmentStatusChanged);
}
