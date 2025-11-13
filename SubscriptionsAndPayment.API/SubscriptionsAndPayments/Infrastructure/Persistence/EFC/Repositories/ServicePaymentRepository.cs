using Microsoft.EntityFrameworkCore;
using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Configuration;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Repositories;

public class ServicePaymentRepository : BaseRepository<ServicePayment>, IServicePaymentRepository
{
    public ServicePaymentRepository(SubscriptionsDbContext context) : base(context)
    {
    }

    public new async Task<ServicePayment?> FindByIdAsync(int id)
    {
        return await Context.Set<ServicePayment>()
            .FirstOrDefaultAsync(sp => sp.Id == id);
    }

    public async Task<ServicePayment?> FindByWorkOrderIdAsync(int workOrderId)
    {
        return await Context.Set<ServicePayment>()
            .FirstOrDefaultAsync(sp => sp.WorkOrderId == workOrderId);
    }

    public async Task<IEnumerable<ServicePayment>> FindByOwnerIdAsync(int ownerId)
    {
        return await Context.Set<ServicePayment>()
            .Where(sp => sp.OwnerId == ownerId)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ServicePayment>> FindByProviderIdAsync(int providerId)
    {
        return await Context.Set<ServicePayment>()
            .Where(sp => sp.ProviderId == providerId)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync();
    }

    public new async Task AddAsync(ServicePayment servicePayment)
    {
        await Context.Set<ServicePayment>().AddAsync(servicePayment);
    }

    public new void Update(ServicePayment servicePayment)
    {
        Context.Set<ServicePayment>().Update(servicePayment);
    }
}
