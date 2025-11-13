using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Shared.Domain.Repositories;

namespace OsitoPolar.Subscriptions.Service.Domain.Repositories;

public interface IPaymentRepository : IBaseRepository<Payment>
{
    Task<Payment?> FindByStripeSessionIdAsync(string stripeSessionId);
    Task<IEnumerable<Payment>> FindByUserIdAsync(int userId);
}