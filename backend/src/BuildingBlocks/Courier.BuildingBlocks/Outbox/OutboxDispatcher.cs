using Courier.BuildingBlocks.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Courier.BuildingBlocks.Outbox;

/// <summary>Background worker that relays outbox rows to the event bus, oldest first, at-least-once.</summary>
public class OutboxDispatcher<TContext> : BackgroundService where TContext : DbContext, IOutboxContext
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OutboxDispatcher<TContext>> _logger;

    public OutboxDispatcher(IServiceScopeFactory scopes, ILogger<OutboxDispatcher<TContext>> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
                await DispatchPendingAsync(db, bus, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Outbox dispatch cycle failed; will retry");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ContinueWith(_ => { });
        }
    }

    /// <summary>Publishes up to <paramref name="batchSize"/> pending messages. Returns how many were published.</summary>
    public static async Task<int> DispatchPendingAsync(TContext db, IEventBus bus, CancellationToken ct, int batchSize = 20)
    {
        var pending = (await db.Outbox
                .Where(m => m.ProcessedAtUtc == null)
                .ToListAsync(ct))
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToList();

        var published = 0;
        foreach (var message in pending)
        {
            try
            {
                await bus.PublishAsync(new EventEnvelope(message.Id, message.Type, message.CreatedAtUtc, message.Payload), ct);
                message.ProcessedAtUtc = DateTime.UtcNow;
                published++;
            }
            catch
            {
                message.Attempts++;
                await db.SaveChangesAsync(ct);
                throw; // stop this cycle to preserve ordering; retried on the next cycle
            }
        }

        await db.SaveChangesAsync(ct);
        return published;
    }
}
