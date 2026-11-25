using Courier.Pricing.Domain;
using Xunit;

namespace Courier.Tests;

public class PricingTests
{
    private static readonly RateCard Standard = new() { ServiceLevel = "Standard", BasePrice = 9.50m, PerKgPrice = 1.80m, EstimatedDays = 4 };
    private static readonly RateCard Express = new() { ServiceLevel = "Express", BasePrice = 14.50m, PerKgPrice = 2.60m, EstimatedDays = 2 };

    [Theory]
    [InlineData("2000", "2100", ZoneResolver.Local)]
    [InlineData("2000", "3000", ZoneResolver.Interstate)]
    [InlineData("3000", "6000", ZoneResolver.Remote)]
    [InlineData("2000", "7000", ZoneResolver.Remote)]
    public void Zone_is_resolved_from_postcodes(string origin, string destination, string expected) =>
        Assert.Equal(expected, ZoneResolver.Resolve(origin, destination));

    [Fact]
    public void Zone_requires_both_postcodes() =>
        Assert.Throws<ArgumentException>(() => ZoneResolver.Resolve("", "3000"));

    [Fact]
    public void Chargeable_weight_uses_the_larger_of_actual_and_volumetric()
    {
        var light = new QuoteRequest(2m, 50m, 40m, 30m, "Standard", "2000", "2000");
        Assert.Equal(12m, QuoteCalculator.ChargeableWeight(light));

        var dense = new QuoteRequest(8m, 10m, 10m, 10m, "Standard", "2000", "2000");
        Assert.Equal(8m, QuoteCalculator.ChargeableWeight(dense));
    }

    [Fact]
    public void Small_local_parcel_costs_base_rate_plus_fuel()
    {
        var quote = QuoteCalculator.CreateDefault().Calculate(
            new QuoteRequest(1m, 10m, 10m, 10m, "Standard", "2000", "2100"), Standard);

        Assert.Equal(10.26m, quote.Total); // 9.50 + 8% fuel (0.76)
        Assert.Equal(2, quote.Lines.Count);
        Assert.Equal("AUD", quote.Currency);
    }

    [Fact]
    public void Interstate_express_parcel_adds_weight_and_zone_charges()
    {
        var quote = QuoteCalculator.CreateDefault().Calculate(
            new QuoteRequest(5m, 20m, 20m, 20m, "Express", "2000", "3000"), Express);

        // 14.50 base + 4 x 2.60 weight + 5.00 interstate = 29.90, fuel 2.39 => 32.29
        Assert.Equal(32.29m, quote.Total);
        Assert.Equal(ZoneResolver.Interstate, quote.Zone);
        Assert.Equal(4, quote.Lines.Count);
    }

    [Fact]
    public void Custom_rule_sets_can_be_composed()
    {
        var calculator = new QuoteCalculator(new IPricingRule[] { new BaseRateRule() });
        var quote = calculator.Calculate(new QuoteRequest(9m, 10m, 10m, 10m, "Standard", "2000", "3000"), Standard);
        Assert.Equal(9.50m, quote.Total);
    }
}
