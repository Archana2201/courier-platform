namespace Courier.Tracking.Domain;

public class TrackingEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    /// <summary>Id of the integration event that produced this row; makes handling idempotent.</summary>
    public Guid SourceEventId { get; set; }
}
