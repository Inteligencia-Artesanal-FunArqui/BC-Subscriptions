using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Model.Commands;

namespace OsitoPolar.Subscriptions.Service.Domain.Services;

public interface ISubscriptionCommandService
{
    Task<Subscription?> Handle(UpgradePlanCommand command);
}