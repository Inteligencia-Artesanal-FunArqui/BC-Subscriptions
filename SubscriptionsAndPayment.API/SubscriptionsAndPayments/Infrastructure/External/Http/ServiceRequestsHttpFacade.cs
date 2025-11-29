using System.Text.Json;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade implementation for communicating with the ServiceRequests microservice.
/// </summary>
public class ServiceRequestsHttpFacade : IServiceRequestsHttpFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceRequestsHttpFacade> _logger;

    public ServiceRequestsHttpFacade(HttpClient httpClient, ILogger<ServiceRequestsHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ServiceRequestData?> GetServiceRequestData(int serviceRequestId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/service-requests/{serviceRequestId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ServiceRequestResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null)
                {
                    return new ServiceRequestData(
                        result.Id,
                        result.ClientId,
                        result.CompanyId,
                        result.Status
                    );
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->ServiceRequests] Error fetching service request {ServiceRequestId}", serviceRequestId);
            return null;
        }
    }

    private record ServiceRequestResponse(
        int Id,
        int ClientId,
        int CompanyId,
        string Status
    );
}
