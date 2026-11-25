using System.Text.Json;
using Courier.BuildingBlocks.Events;
using Courier.BuildingBlocks.Outbox;
using Courier.Shipments.Data;
using Courier.Tracking.Data;
using Courier.Tracking.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Courier.Tests;

public class MessagingTests
{
    private sealed class RecordingBus : IEventBus
    {
        public List<EventEnvelope> Published { get; } = new();
        public bool Fail { get; set; }

        public Task PublishAsync(EventEnvelope envelope, CancellationToken ct = default)
        {
            if (Fail) throw new HttpRequestException("subscriber down");
            Published.Add(envelope);
            return Task.CompletedTask;
        }
    }

    private static (SqliteConnection, TContext) Create<TContext>(Func<DbContextOptions<TContext>, TContext> factory)
        where TContext : DbContext
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = factory(new DbContextOptionsBuilder<TContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        return (connection, context);
    }

    [Fact]
    public async Task Outbox_dispatcher_publishes_pending_messages_once()
    {
        var (connection, db) = Create<ShipmentsDbContext>(o => new ShipmentsDbContext(o));
        using var _ = connection;
        await using var __ = db;

        db.Outbox.Add(OutboxMessage.From(EventTypes.ShipmentCreated,
            new ShipmentCreated(Guid.NewGuid(), "CP1", "Sydney", "Melbourne", DateTime.UtcNow)));
        await db.SaveChangesAsync();

        var bus = new RecordingBus();
        var first = await OutboxDispatcher<ShipmentsDbContext>.DispatchPendingAsync(db, bus, default);
        var second = await OutboxDispatcher<ShipmentsDbContext>.DispatchPendingAsync(db, bus, default);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Single(bus.Published);
    }

    [Fact]
    public async Task Outbox_dispatcher_keeps_message_and_counts_attempt_when_bus_fails()
    {
        var (connection, db) = Create<ShipmentsDbContext>(o => new ShipmentsDbContext(o));
        using var _ = connection;
        await using var __ = db;

        db.Outbox.Add(OutboxMessage.From(EventTypes.ShipmentCreated,
            new ShipmentCreated(Guid.NewGuid(), "CP1", "A", "B", DateTime.UtcNow)));
        await db.SaveChangesAsync();

        var bus = new RecordingBus { Fail = true };
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            OutboxDispatcher<ShipmentsDbContext>.DispatchPendingAsync(db, bus, default));

        var message = await db.Outbox.SingleAsync();
        Assert.Null(message.ProcessedAtUtc);
        Assert.Equal(1, message.Attempts);

        bus.Fail = false;
        Assert.Equal(1, await OutboxDispatcher<ShipmentsDbContext>.DispatchPendingAsync(db, bus, default));
    }

    [Fact]
    public async Task Tracking_handler_applies_events_and_ignores_duplicates()
    {
        var (connection, db) = Create<TrackingDbContext>(o => new TrackingDbContext(o));
        using var _ = connection;
        await using var __ = db;
        var handler = new TrackingEventHandler(db);

        var created = new ShipmentCreated(Guid.NewGuid(), "CP260101ABCDEF", "Sydney", "Melbourne", DateTime.UtcNow);
        var envelope = new EventEnvelope(Guid.NewGuid(), EventTypes.ShipmentCreated, DateTime.UtcNow, JsonSerializer.Serialize(created));

        Assert.True(await handler.HandleAsync(envelope));
        Assert.False(await handler.HandleAsync(envelope)); // redelivery is a no-op

        var changed = new ShipmentStatusChanged(created.ShipmentId, created.TrackingNumber, "PickedUp", "Sydney", DateTime.UtcNow.AddMinutes(5));
        Assert.True(await handler.HandleAsync(new EventEnvelope(Guid.NewGuid(), EventTypes.ShipmentStatusChanged,
            DateTime.UtcNow, JsonSerializer.Serialize(changed))));

        var rows = await db.TrackingEvents.OrderBy(t => t.OccurredAtUtc).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal("PickedUp", rows[1].Status);
    }

    [Fact]
    public async Task Tracking_handler_ignores_unknown_event_types()
    {
        var (connection, db) = Create<TrackingDbContext>(o => new TrackingDbContext(o));
        using var _ = connection;
        await using var __ = db;

        var applied = await new TrackingEventHandler(db).HandleAsync(
            new EventEnvelope(Guid.NewGuid(), "SomethingElse", DateTime.UtcNow, "{}"));
        Assert.False(applied);
    }
}
