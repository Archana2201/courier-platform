using System.Net.Http.Json;

namespace Courier.Shipments.Clients;

public record PricingRequest(decimal WeightKg, decimal LengthCm, decimal WidthCm, decimal HeightCm,
    string ServiceLevel, string OriginPostcode, string DestinationPostcode);

public record PricingResponse(string ServiceLevel, string Zone, decimal ChargeableWeightKg, int EstimatedDays,
    decimal Total, string Currency);

/// <summary>Port to the Pricing service; the HTTP adapter below is replaceable in tests.</summary>
public interface IPricingClient
{
    Task<PricingResponse> GetQuoteAsync(PricingRequest request, CancellationToken ct = default);
}

public class HttpPricingClient : IPricingClient
{
    private readonly HttpClient _http;

    public HttpPricingClient(HttpClient http) => _http = http;

    public async Task<PricingResponse> GetQuoteAsync(PricingRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/quotes", request, ct);
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<PricingResponse>(cancellationToken: ct);
        return quote ?? throw new InvalidOperationException("Pricing service returned an empty response.");
    }
}
