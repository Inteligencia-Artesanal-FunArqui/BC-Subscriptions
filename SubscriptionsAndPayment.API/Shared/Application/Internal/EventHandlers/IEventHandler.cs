using Cortex.Mediator.Notifications;
using OsitoPolar.Subscriptions.Service.Shared.Domain.Model.Events;

namespace OsitoPolar.Subscriptions.Service.Shared.Application.Internal.EventHandlers;

public interface IEventHandler<in TEvent> : INotificationHandler<TEvent> where TEvent : IEvent
{
    
}