using OsitoPolar.Subscriptions.Service.Domain.Model.Commands;

namespace OsitoPolar.Subscriptions.Service.Interfaces.REST.Transform;

public static class UpgradeSubscriptionCommandFromResourceAssembler
{
    public static UpgradePlanCommand ToCommandFromResource(UpgradeSubscriptionResource resource)
    {
        return new UpgradePlanCommand(resource.UserId, resource.PlanId);
    }
}