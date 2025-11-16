using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Model.Commands;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Domain.Services;
using OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;
using OsitoPolar.Subscriptions.Service.Shared.Domain.Repositories;

namespace OsitoPolar.Subscriptions.Service.Application.Internal.CommandServices;

public class SubscriptionCommandService(
    ISubscriptionRepository subscriptionRepository,
    IProfilesHttpFacade profilesHttpFacade,
    ILogger<SubscriptionCommandService> logger) : ISubscriptionCommandService
{
    public async Task<Subscription?> Handle(UpgradePlanCommand command)
    {
        try
        {
            logger.LogInformation(
                "Processing subscription upgrade for UserId: {UserId} to PlanId: {PlanId}",
                command.UserId, command.PlanId);

            // Get the new subscription plan
            var newPlan = await subscriptionRepository.FindByIdAsync(command.PlanId)
                          ?? throw new InvalidOperationException($"Plan {command.PlanId} not found");

            logger.LogInformation(
                "Found plan: {PlanName} with MaxEquipment: {MaxEquipment}",
                newPlan.PlanName, newPlan.MaxEquipment);

            // Update the owner's plan in the Profiles microservice via HTTP facade
            var maxUnits = newPlan.MaxEquipment ?? 10; // Default to 10 if not specified
            var updateSuccess = await profilesHttpFacade.UpdateOwnerPlanAsync(
                command.UserId,
                command.PlanId,
                maxUnits);

            if (!updateSuccess)
            {
                logger.LogError(
                    "Failed to update owner plan in Profiles service for UserId: {UserId}",
                    command.UserId);
                throw new InvalidOperationException(
                    "Failed to update owner profile with new subscription plan");
            }

            logger.LogInformation(
                "Successfully upgraded subscription for UserId: {UserId} to plan: {PlanName}",
                command.UserId, newPlan.PlanName);

            return newPlan;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error upgrading subscription for UserId: {UserId} to PlanId: {PlanId}",
                command.UserId, command.PlanId);

            return null;
        }
    }
}