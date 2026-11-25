using Courier.Shipments.Domain;
using Xunit;

namespace Courier.Tests;

public class ShipmentDomainTests
{
    private sealed class FixedTracking : ITrackingNumberGenerator
    {
        public string Next(DateTime nowUtc) => "CP000000AAAAAA";
    }

    private static Address Addr(string name = "Alex", string postcode = "2000") =>
        new() { Name = name, Line1 = "1 Test St", City = "Sydney", State = "NSW", Postcode = postcode };

    private static Shipment NewShipment() =>
        new ShipmentFactory(new FixedTracking()).Create(
            new CreateShipmentCommand(Addr(), Addr("Sam", "3000"), 2m, 10m, 10m, 10m, "Standard"),
            12.34m, "AUD", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Factory_creates_a_shipment_in_created_state_with_history()
    {
        var shipment = NewShipment();
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
        Assert.Equal("CP000000AAAAAA", shipment.TrackingNumber);
        Assert.Single(shipment.History);
        Assert.Equal(12.34m, shipment.Price);
    }

    [Fact]
    public void Factory_rejects_missing_recipient()
    {
        var factory = new ShipmentFactory(new FixedTracking());
        Assert.Throws<DomainException>(() => factory.Create(
            new CreateShipmentCommand(Addr(), new Address(), 1m, 1m, 1m, 1m, "Standard"), 1m, "AUD", DateTime.UtcNow));
    }

    [Fact]
    public void Shipment_follows_the_happy_path_to_delivered()
    {
        var shipment = NewShipment();
        var now = DateTime.UtcNow;
        shipment.MoveTo(ShipmentStatus.PickedUp, "Sydney", now);
        shipment.MoveTo(ShipmentStatus.InTransit, "Hub", now);
        shipment.MoveTo(ShipmentStatus.OutForDelivery, "Melbourne", now);
        shipment.MoveTo(ShipmentStatus.Delivered, "Melbourne", now);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(5, shipment.History.Count);
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.OutForDelivery)]
    public void Created_shipment_cannot_skip_pickup(ShipmentStatus target)
    {
        var shipment = NewShipment();
        Assert.Throws<DomainException>(() => shipment.MoveTo(target, null, DateTime.UtcNow));
    }

    [Fact]
    public void Created_shipment_can_be_cancelled_but_cancelled_is_terminal()
    {
        var shipment = NewShipment();
        shipment.MoveTo(ShipmentStatus.Cancelled, null, DateTime.UtcNow);
        Assert.Throws<DomainException>(() => shipment.MoveTo(ShipmentStatus.PickedUp, null, DateTime.UtcNow));
    }

    [Fact]
    public void Tracking_number_generator_produces_expected_format()
    {
        var number = new TrackingNumberGenerator().Next(new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc));
        Assert.StartsWith("CP260305", number);
        Assert.Equal(14, number.Length);
    }
}
