namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade for communicating with the Profiles microservice.
/// Enables cross-service communication following the Anti-Corruption Layer pattern.
/// </summary>
public interface IProfilesHttpFacade
{
    /// <summary>
    /// Fetch Owner ID by User ID
    /// </summary>
    Task<int> FetchOwnerIdByUserId(int userId);

    /// <summary>
    /// Fetch Provider ID by User ID
    /// </summary>
    Task<int> FetchProviderIdByUserId(int userId);

    /// <summary>
    /// Updates the owner's subscription plan
    /// </summary>
    Task<bool> UpdateOwnerPlan(int ownerId, int newPlanId, int maxUnits);

    /// <summary>
    /// Updates the provider's subscription plan
    /// </summary>
    Task<bool> UpdateProviderPlan(int providerId, int newPlanId, int maxClients);

    /// <summary>
    /// Updates the provider's balance
    /// </summary>
    Task<bool> UpdateProviderBalance(int providerId, decimal amount);

    /// <summary>
    /// Get owner name by owner ID
    /// </summary>
    Task<(string firstName, string lastName)?> GetOwnerNameByOwnerId(int ownerId);

    /// <summary>
    /// Get provider profile for auth by user ID
    /// </summary>
    Task<(string companyName, decimal balance)?> GetProviderProfileForAuthByUserId(int userId);

    /// <summary>
    /// Fetch provider company name
    /// </summary>
    Task<string?> FetchProviderCompanyName(int providerId);

    /// <summary>
    /// Get provider user ID by provider ID
    /// </summary>
    Task<int> GetProviderUserIdByProviderId(int providerId);
}
