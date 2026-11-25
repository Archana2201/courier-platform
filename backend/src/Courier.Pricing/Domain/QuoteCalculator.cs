namespace Courier.Pricing.Domain;

public class QuoteCalculator
{
    public const decimal VolumetricDivisor = 5000m; // cm^3 per kg, an industry-standard divisor
    public const decimal MaxWeightKg = 70m;
    public static readonly string[] ServiceLevels = { "Standard", "Express", "Overnight" };

    private readonly IReadOnlyList<IPricingRule> _rules;

    public QuoteCalculator(IEnumerable<IPricingRule> rules) => _rules = rules.ToList();

    public static QuoteCalculator CreateDefault() => new(new IPricingRule[]
    {
        new BaseRateRule(), new WeightRule(), new ZoneSurchargeRule(), new FuelSurchargeRule(),
    });

    public static decimal ChargeableWeight(QuoteRequest r)
    {
        var volumetric = r.LengthCm * r.WidthCm * r.HeightCm / VolumetricDivisor;
        return Math.Max(r.WeightKg, volumetric);
    }

    public QuoteResult Calculate(QuoteRequest request, RateCard rateCard)
    {
        var chargeable = ChargeableWeight(request);
        var zone = ZoneResolver.Resolve(request.OriginPostcode, request.DestinationPostcode);
        var context = new PricingContext(request, rateCard, zone, chargeable);

        var lines = new List<QuoteLine>();
        var subtotal = 0m;
        foreach (var rule in _rules)
        {
            var line = rule.Apply(context, subtotal);
            if (line is null) continue;
            lines.Add(line);
            subtotal += line.Amount;
        }

        return new QuoteResult(rateCard.ServiceLevel, zone, Math.Round(chargeable, 2), rateCard.EstimatedDays,
            lines, Math.Round(subtotal, 2), "AUD");
    }
}
