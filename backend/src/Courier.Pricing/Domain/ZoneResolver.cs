namespace Courier.Pricing.Domain;

/// <summary>Maps an origin/destination postcode pair to a pricing zone (Australian postcodes).</summary>
public static class ZoneResolver
{
    public const string Local = "Local";
    public const string Interstate = "Interstate";
    public const string Remote = "Remote";

    // First digit of an AU postcode: 0/8 NT, 2 NSW/ACT, 3 VIC, 4 QLD, 5 SA, 6 WA, 7 TAS.
    private static readonly HashSet<char> RemoteRegions = new() { '0', '6', '7', '8' };

    public static string Resolve(string originPostcode, string destinationPostcode)
    {
        if (string.IsNullOrWhiteSpace(originPostcode) || string.IsNullOrWhiteSpace(destinationPostcode))
            throw new ArgumentException("Both postcodes are required.");

        var origin = originPostcode.Trim()[0];
        var destination = destinationPostcode.Trim()[0];

        if (origin == destination) return Local;
        return RemoteRegions.Contains(destination) ? Remote : Interstate;
    }
}
