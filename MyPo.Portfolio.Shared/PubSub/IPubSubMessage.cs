namespace MyPo.Portfolio.Shared.PubSub;

/// <summary>
/// Marker contract for messages published through the application pub/sub bus.
/// </summary>
public interface IPubSubMessage
{
    Guid MessageId { get; }

    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Base type for application pub/sub messages.
/// </summary>
public abstract record PubSubMessage : IPubSubMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
