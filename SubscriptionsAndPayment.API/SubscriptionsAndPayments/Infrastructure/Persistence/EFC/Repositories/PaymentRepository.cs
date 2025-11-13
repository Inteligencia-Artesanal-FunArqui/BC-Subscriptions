using Microsoft.EntityFrameworkCore;
using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Configuration;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Repositories;

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(SubscriptionsDbContext context) : base(context)
    {
    }

    public async Task<Payment?> FindByStripeSessionIdAsync(string stripeSessionId)
    {
        return await Context.Set<Payment>()
            .Where(p => EF.Property<string>(p, "StripeSessionId") == stripeSessionId)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Payment>> FindByUserIdAsync(int userId)
    {
        return await Context.Set<Payment>()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }
}