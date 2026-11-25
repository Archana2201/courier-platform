namespace Courier.Shipments.Domain;

public class Address
{
    public string Name { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
}

public class ShipmentStatusChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShipmentId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string? Location { get; set; }
    public DateTime AtUtc { get; set; }
}

public class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TrackingNumber { get; set; } = string.Empty;
    public Address Sender { get; set; } = new();
    public Address Recipient { get; set; } = new();
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public string ServiceLevel { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "AUD";
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;
    public DateTime CreatedAtUtc { get; set; }
    public List<ShipmentStatusChange> History { get; set; } = new();

    /// <summary>Moves the shipment to a new status if the current state allows it.</summary>
    public ShipmentStatusChange MoveTo(ShipmentStatus next, string? location, DateTime nowUtc)
    {
        var state = ShipmentState.For(Status);
        if (!state.CanMoveTo(next))
            throw new DomainException($"Cannot move a shipment from {Status} to {next}.");

        Status = next;
        var change = new ShipmentStatusChange { ShipmentId = Id, Status = next, Location = location, AtUtc = nowUtc };
        History.Add(change);
        return change;
    }
}
