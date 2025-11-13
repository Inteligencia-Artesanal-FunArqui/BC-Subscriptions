using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Model.Queries;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Domain.Services;

namespace OsitoPolar.Subscriptions.Service.Application.Internal.QueryServices;

public class SubscriptionQueryService(ISubscriptionRepository subscriptionRepository) : ISubscriptionQueryService
{
    public async Task<Subscription?> Handle(GetSubscriptionByIdQuery query)
    {
        return await subscriptionRepository.FindByIdAsync(query.SubscriptionId);
    }

    public async Task<IEnumerable<Subscription>> Handle(GetPlansQuery query)
    {
        var plans = await subscriptionRepository.ListAsync();
        // Provider plans: IDs 4-6 (based on MaxClients being present or unlimited)
        // Owner plans: IDs 1-3 (based on MaxEquipment being present)
        return query.UserType.ToLower() == "provider"
            ? plans.Where(p => p.Id >= 4) // Provider plans (including unlimited)
            : plans.Where(p => p.Id <= 3);  // Owner plans
    }
}