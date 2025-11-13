using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Configuration;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Repositories;

public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(SubscriptionsDbContext context) : base(context)
    {
    }

    public Task<Subscription?> FindByUserIdAsync(int userId)
    {
        // For now, it returns null to the users that do not have an active subscription
        // This will allow the pay flow to work correctly
        return Task.FromResult<Subscription?>(null);


        // return await Context.Set<Subscription>().FirstOrDefaultAsync(s => s.Id == 1);
    }
}