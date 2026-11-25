namespace Courier.Shipments.Domain;

public record CreateShipmentCommand(
    Address Sender,
    Address Recipient,
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    string ServiceLevel);

public interface ITrackingNumberGenerator
{
    string Next(DateTime nowUtc);
}

public class TrackingNumberGenerator : ITrackingNumberGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Next(DateTime nowUtc)
    {
        var chars = new char[6];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        return $"CP{nowUtc:yyMMdd}{new string(chars)}";
    }
}

/// <summary>Factory: the single place that builds a valid new Shipment.</summary>
public class ShipmentFactory
{
    private readonly ITrackingNumberGenerator _tracking;

    public ShipmentFactory(ITrackingNumberGenerator tracking) => _tracking = tracking;

    public Shipment Create(CreateShipmentCommand cmd, decimal price, string currency, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(cmd.Recipient.Name) || string.IsNullOrWhiteSpace(cmd.Recipient.Postcode))
            throw new DomainException("Recipient name and postcode are required.");
        if (string.IsNullOrWhiteSpace(cmd.Sender.Name) || string.IsNullOrWhiteSpace(cmd.Sender.Postcode))
            throw new DomainException("Sender name and postcode are required.");

        var shipment = new Shipment
        {
            TrackingNumber = _tracking.Next(nowUtc),
            Sender = cmd.Sender,
            Recipient = cmd.Recipient,
            WeightKg = cmd.WeightKg,
            LengthCm = cmd.LengthCm,
            WidthCm = cmd.WidthCm,
            HeightCm = cmd.HeightCm,
            ServiceLevel = cmd.ServiceLevel,
            Price = price,
            Currency = currency,
            Status = ShipmentStatus.Created,
            CreatedAtUtc = nowUtc,
        };
        shipment.History.Add(new ShipmentStatusChange
        {
            ShipmentId = shipment.Id,
            Status = ShipmentStatus.Created,
            Location = shipment.Sender.City,
            AtUtc = nowUtc,
        });
        return shipment;
    }
}
