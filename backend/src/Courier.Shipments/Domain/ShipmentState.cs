namespace Courier.Shipments.Domain;

public enum ShipmentStatus { Created, PickedUp, InTransit, OutForDelivery, Delivered, Cancelled }

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>State pattern: each status knows which transitions are legal.</summary>
public abstract class ShipmentState
{
    public abstract ShipmentStatus Status { get; }
    public abstract IReadOnlyCollection<ShipmentStatus> AllowedNext { get; }

    public bool CanMoveTo(ShipmentStatus next) => AllowedNext.Contains(next);

    public static ShipmentState For(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Created => new CreatedState(),
        ShipmentStatus.PickedUp => new PickedUpState(),
        ShipmentStatus.InTransit => new InTransitState(),
        ShipmentStatus.OutForDelivery => new OutForDeliveryState(),
        ShipmentStatus.Delivered => new DeliveredState(),
        ShipmentStatus.Cancelled => new CancelledState(),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
}

public sealed class CreatedState : ShipmentState
{
    public override ShipmentStatus Status => ShipmentStatus.Created;
    public override IReadOnlyCollection<ShipmentStatus> AllowedNext { get; } =
        new[] { ShipmentStatus.PickedUp, ShipmentStatus.Cancelled };
}

public sealed class PickedUpState : ShipmentState
{
    public override ShipmentStatus Status => ShipmentStatus.PickedUp;
    public override IReadOnlyCollection<ShipmentStatus> AllowedNext { get; } =
        new[] { ShipmentStatus.InTransit };
}

public sealed class InTransitState : ShipmentState
{
    public override ShipmentStatus Status => ShipmentStatus.InTransit;
    public override IReadOnlyCollection<ShipmentStatus> AllowedNext { get; } =
        new[] { ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery };
}

public sealed class OutForDeliveryState : ShipmentState
{
    public override ShipmentStatus Status => ShipmentStatus.OutForDelivery;
    public override IReadOnlyCollection<ShipmentStatus> AllowedNext { get; } =
        new[] { ShipmentStatus.Delivered, ShipmentStatus.InTransit };
}

public sealed class DeliveredState : ShipmentState
{
    public override ShipmentStatus Status => ShipmentStatus.Delivered;
    public override IReadOnlyCollection<ShipmentStatus> AllowedNext { get; } = Array.Empty<ShipmentStatus>();
}

public sealed class CancelledState : ShipmentState
{
    public override ShipmentStatus Status => ShipmentStatus.Cancelled;
    public override IReadOnlyCollection<ShipmentStatus> AllowedNext { get; } = Array.Empty<ShipmentStatus>();
}
