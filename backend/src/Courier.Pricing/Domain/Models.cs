namespace Courier.Pricing.Domain;

public class RateCard
{
    public int Id { get; set; }
    public string ServiceLevel { get; set; } = string.Empty;   // Standard | Express | Overnight
    public decimal BasePrice { get; set; }                      // covers the first kg
    public decimal PerKgPrice { get; set; }                     // each additional chargeable kg
    public int EstimatedDays { get; set; }
}

public record QuoteRequest(
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    string ServiceLevel,
    string OriginPostcode,
    string DestinationPostcode);

public record QuoteLine(string Description, decimal Amount);

public record QuoteResult(
    string ServiceLevel,
    string Zone,
    decimal ChargeableWeightKg,
    int EstimatedDays,
    IReadOnlyList<QuoteLine> Lines,
    decimal Total,
    string Currency);

/// <summary>Everything a rule needs to price one parcel.</summary>
public record PricingContext(QuoteRequest Request, RateCard RateCard, string Zone, decimal ChargeableWeightKg);
