using System.Text;
using System.Text.Json;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade implementation for communicating with the Profiles microservice.
/// Uses HttpClient to make REST API calls following the Anti-Corruption Layer pattern.
/// </summary>
public class ProfilesHttpFacade : IProfilesHttpFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfilesHttpFacade> _logger;

    public ProfilesHttpFacade(HttpClient httpClient, ILogger<ProfilesHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<int> FetchOwnerIdByUserId(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/owners/by-user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OwnerResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Id ?? 0;
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Profiles] Error fetching owner ID for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<int> FetchProviderIdByUserId(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/providers/by-user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProviderResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Id ?? 0;
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Profiles] Error fetching provider ID for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> UpdateOwnerPlan(int ownerId, int newPlanId, int maxUnits)
    {
        try
        {
            var requestBody = new { planId = newPlanId, maxUnits = maxUnits };
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/api/v1/profiles/owners/{ownerId}/plan", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Subscriptions->Profiles] Updated owner {OwnerId} plan to {PlanId}", ownerId, newPlanId);
                return true;
            }

            _logger.LogWarning("[Subscriptions->Profiles] Failed to update owner plan: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Profiles] Error updating owner plan");
            return false;
        }
    }

    public async Task<bool> UpdateProviderPlan(int providerId, int newPlanId, int maxClients)
    {
        try
        {
            var requestBody = new { planId = newPlanId, maxClients = maxClients };
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/api/v1/profiles/providers/{providerId}/plan", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Subscriptions->Profiles] Updated provider {ProviderId} plan to {PlanId}", providerId, newPlanId);
                return true;
            }

            _logger.LogWarning("[Subscriptions->Profiles] Failed to update provider plan: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Profiles] Error updating provider plan");
            return false;
        }
    }

    public async Task<bool> UpdateProviderBalance(int providerId, decimal amount)
    {
        try
        {
            var requestBody = new { amount = amount, description = "Service revenue" };
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/v1/profiles/providers/{providerId}/balance", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Subscriptions->Profiles] Updated provider {ProviderId} balance by {Amount}", providerId, amount);
                return true;
            }

            _logger.LogWarning("[Subscriptions->Profiles] Failed to update provider balance: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Profiles] Error updating provider balance");
            return false;
        }
    }

    public async Task<(string firstName, string lastName)?> GetOwnerNameByOwnerId(int ownerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/owners/{ownerId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OwnerFullResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null)
                {
                    return (result.FirstName, result.LastName);
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Profiles] Error fetching owner name for {OwnerId}", ownerId);
            return null;
        }
    }

    public async Task<(string companyName, decimal balance)?> GetProviderProfileForAuthByUserId(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/providers/by-user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProviderFullResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null)
                {
                    return (result.CompanyName, result.Balance);
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Profiles] Error fetching provider profile for user {UserId}", userId);
            return null;
        }
    }

    public async Task<string?> FetchProviderCompanyName(int providerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/providers/{providerId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProviderFullResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.CompanyName;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Profiles] Error fetching provider company name for {ProviderId}", providerId);
            return null;
        }
    }

    public async Task<int> GetProviderUserIdByProviderId(int providerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/providers/{providerId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProviderFullResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.UserId ?? 0;
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Profiles] Error fetching provider user ID for {ProviderId}", providerId);
            return 0;
        }
    }

    private record OwnerResponse(int Id);
    private record ProviderResponse(int Id);
    private record OwnerFullResponse(int Id, string FirstName, string LastName, int UserId);
    private record ProviderFullResponse(int Id, string CompanyName, decimal Balance, int UserId);
}
