namespace MyPo.Portfolio.Shared.PubSub;

/// <summary>
/// Publishes application messages without exposing the underlying messaging framework.
/// </summary>
public interface IPubSubPublisher
{
    ValueTask PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IPubSubMessage;
}
