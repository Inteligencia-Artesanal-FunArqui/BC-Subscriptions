using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Model.Commands;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Domain.Services;
using OsitoPolar.Subscriptions.Service.Shared.Domain.Repositories;

namespace OsitoPolar.Subscriptions.Service.Application.Internal.CommandServices;

public class SubscriptionCommandService(
    ISubscriptionRepository subscriptionRepository) : ISubscriptionCommandService
{
    public async Task<Subscription?> Handle(UpgradePlanCommand command)
    {
        try
        {
            
            var newPlan = await subscriptionRepository.FindByIdAsync(command.PlanId)
                          ?? throw new InvalidOperationException($"Plan {command.PlanId} not found");
            
            Console.WriteLine($"User {command.UserId} upgraded to plan {newPlan.PlanName}");
            
            // TODO: Implement real logic to update the user's subscription in the database
            
            return newPlan;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in upgrade: {ex.Message}");
            
            return null;
        }
    }
}