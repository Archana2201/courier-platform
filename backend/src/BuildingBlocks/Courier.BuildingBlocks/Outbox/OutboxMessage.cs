using System.Text.Json;
using Courier.BuildingBlocks.Events;
using Microsoft.EntityFrameworkCore;

namespace Courier.BuildingBlocks.Outbox;

/// <summary>A pending integration event stored in the same transaction as the business change (Transactional Outbox).</summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public int Attempts { get; set; }

    public static OutboxMessage From<T>(string type, T payload) => new()
    {
        Type = type,
        Payload = JsonSerializer.Serialize(payload),
    };
}

public interface IOutboxContext
{
    DbSet<OutboxMessage> Outbox { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
