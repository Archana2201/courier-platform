namespace Courier.Pricing.Domain;

/// <summary>Strategy: one pricing concern. Rules run in order and may read the running subtotal.</summary>
public interface IPricingRule
{
    QuoteLine? Apply(PricingContext context, decimal subtotal);
}

public class BaseRateRule : IPricingRule
{
    public QuoteLine? Apply(PricingContext context, decimal subtotal) =>
        new($"{context.RateCard.ServiceLevel} base rate (first kg)", context.RateCard.BasePrice);
}

public class WeightRule : IPricingRule
{
    public QuoteLine? Apply(PricingContext context, decimal subtotal)
    {
        var extraKg = Math.Ceiling(context.ChargeableWeightKg) - 1m;
        if (extraKg <= 0) return null;
        return new($"Additional weight ({extraKg} kg x {context.RateCard.PerKgPrice:0.00})", extraKg * context.RateCard.PerKgPrice);
    }
}

public class ZoneSurchargeRule : IPricingRule
{
    public QuoteLine? Apply(PricingContext context, decimal subtotal) => context.Zone switch
    {
        ZoneResolver.Interstate => new("Interstate surcharge", 5.00m),
        ZoneResolver.Remote => new("Remote area surcharge", 12.00m),
        _ => null,
    };
}

public class FuelSurchargeRule : IPricingRule
{
    public const decimal Rate = 0.08m;

    public QuoteLine? Apply(PricingContext context, decimal subtotal) =>
        new($"Fuel surcharge ({Rate:P0})", Math.Round(subtotal * Rate, 2));
}
