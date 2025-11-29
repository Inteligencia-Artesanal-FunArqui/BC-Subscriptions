using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Interfaces.ACL;

namespace OsitoPolar.Subscriptions.Service.Application.ACL;

/// <summary>
/// Facade implementation for the Subscriptions and Payments context
/// NOTA: Este facade NO debe depender de Profiles (microservicio separado)
/// Solo retorna datos de subscriptions/planes
/// </summary>
public class SubscriptionContextFacade : ISubscriptionContextFacade
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public SubscriptionContextFacade(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<int> GetUserSubscriptionPlanId(int userId)
    {
        // NOTA: En microservicios, Subscriptions NO tiene acceso a la tabla owners
        // El userId→planId está en Profiles service
        // Este método se puede deprecar o hacer que retorne 0
        // El IAM debería obtener planId desde Profiles, no desde Subscriptions
        return 0; // No tenemos acceso a owners en este microservicio
    }

    public async Task<bool> CanUserAddMoreClients(int userId)
    {
        // NOTA: Similar al anterior, no tenemos acceso a owners
        // Esta lógica debería estar en Profiles service
        return true; // Placeholder - Profiles service debe validar esto
    }

    public decimal CalculateServiceCommission(decimal amount)
    {
        return amount * 0.15m; // 15% commission
    }

    public async Task<string> FetchSubscriptionPlanName(int planId)
    {
        var plan = await _subscriptionRepository.FindByIdAsync(planId);
        return plan?.PlanName ?? string.Empty;
    }

    public async Task<(int planId, string planName, decimal price, int maxClients)?> GetSubscriptionDataById(int planId)
    {
        var plan = await _subscriptionRepository.FindByIdAsync(planId);
        if (plan == null) return null;

        return (plan.Id, plan.PlanName, plan.Price.Amount, plan.MaxClients ?? 0);
    }

    public async Task<(int maxEquipment, int maxClients)?> GetSubscriptionLimits(int planId)
    {
        var plan = await _subscriptionRepository.FindByIdAsync(planId);
        if (plan == null) return null;

        return (plan.MaxEquipment ?? 10, plan.MaxClients ?? 50);
    }

    public async Task<(int planId, string planName, decimal price, string currency, int? maxEquipment, int? maxClients)?> GetFullSubscriptionData(int planId)
    {
        var plan = await _subscriptionRepository.FindByIdAsync(planId);
        if (plan == null) return null;

        return (plan.Id, plan.PlanName, plan.Price.Amount, plan.Price.Currency, plan.MaxEquipment, plan.MaxClients);
    }
}
