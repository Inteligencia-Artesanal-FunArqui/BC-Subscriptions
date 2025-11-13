using OsitoPolar.Subscriptions.Service.Shared.Domain.Model.Events;

namespace OsitoPolar.Subscriptions.Service.Shared.Domain.Services;

/// <summary>
/// Event Bus interface for publishing domain events across bounded contexts
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered handlers
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="event">The event instance to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}
