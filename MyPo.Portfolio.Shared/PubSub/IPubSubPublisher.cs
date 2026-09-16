namespace MyPo.Portfolio.Shared.PubSub;

/// <summary>
/// Publishes application messages without exposing the underlying messaging framework.
/// </summary>
public interface IPubSubPublisher
{
    /// <summary>
    /// Publishes the specified application message asynchronously.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IPubSubMessage;
}
