using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Courier.BuildingBlocks.Events;

/// <summary>Port for publishing integration events. Swap the implementation for Kafka/RabbitMQ without touching callers.</summary>
public interface IEventBus
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken ct = default);
}

public class EventBusOptions
{
    /// <summary>Full URLs that receive every published envelope via HTTP POST.</summary>
    public List<string> Subscribers { get; set; } = new();
}

/// <summary>Simple HTTP implementation of <see cref="IEventBus"/> used for local development and demos.</summary>
public class HttpEventBus : IEventBus
{
    private readonly HttpClient _http;
    private readonly EventBusOptions _options;

    public HttpEventBus(HttpClient http, IOptions<EventBusOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task PublishAsync(EventEnvelope envelope, CancellationToken ct = default)
    {
        foreach (var url in _options.Subscribers)
        {
            var response = await _http.PostAsJsonAsync(url, envelope, ct);
            response.EnsureSuccessStatusCode();
        }
    }
}
