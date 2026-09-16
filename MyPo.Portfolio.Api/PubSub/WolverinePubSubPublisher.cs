using MyPo.Portfolio.Shared.PubSub;
using Wolverine;

namespace MyPo.Portfolio.Api.PubSub;

/// <summary>
/// Implements the IPubSubPublisher interface using Wolverine's message bus.
/// </summary>
/// <param name="messageBus">The Wolverine message bus used to publish messages.</param>
public sealed class WolverinePubSubPublisher(IMessageBus messageBus) : IPubSubPublisher
{
    /// <inheritdoc />
    public ValueTask PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IPubSubMessage
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();
        return messageBus.PublishAsync(message);
    }
}
