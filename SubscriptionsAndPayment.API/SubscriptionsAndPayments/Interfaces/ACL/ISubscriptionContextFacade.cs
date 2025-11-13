namespace OsitoPolar.Subscriptions.Service.Interfaces.ACL;

/// <summary>
/// Facade for the Subscriptions and Payments context
/// </summary>
public interface ISubscriptionContextFacade
{
    /// <summary>
    /// Get user's subscription plan ID
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>Subscription Plan ID if found, 0 otherwise</returns>
    Task<int> GetUserSubscriptionPlanId(int userId);

    /// <summary>
    /// Check if user can add more clients based on their subscription plan
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>True if user can add more clients, false otherwise</returns>
    Task<bool> CanUserAddMoreClients(int userId);

    /// <summary>
    /// Calculate service commission (15% of amount)
    /// </summary>
    /// <param name="amount">Service amount</param>
    /// <returns>Commission amount</returns>
    decimal CalculateServiceCommission(decimal amount);

    /// <summary>
    /// Get subscription plan name
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <returns>Plan name if found, empty string otherwise</returns>
    Task<string> FetchSubscriptionPlanName(int planId);

    /// <summary>
    /// Get subscription data by plan ID
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <returns>Tuple with (planId, planName, price, maxClients) or null if not found</returns>
    Task<(int planId, string planName, decimal price, int maxClients)?> GetSubscriptionDataById(int planId);

    /// <summary>
    /// Get subscription plan limits for registration
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <returns>Tuple with (maxEquipment, maxClients) or null if not found</returns>
    Task<(int maxEquipment, int maxClients)?> GetSubscriptionLimits(int planId);

    /// <summary>
    /// Get full subscription data including all fields
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <returns>Tuple with all subscription fields or null if not found</returns>
    Task<(int planId, string planName, decimal price, string currency, int? maxEquipment, int? maxClients)?> GetFullSubscriptionData(int planId);
}
