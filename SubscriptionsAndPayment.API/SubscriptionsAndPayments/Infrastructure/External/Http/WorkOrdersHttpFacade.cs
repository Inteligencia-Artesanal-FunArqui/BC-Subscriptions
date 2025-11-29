using System.Text.Json;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade implementation for communicating with the WorkOrders microservice.
/// </summary>
public class WorkOrdersHttpFacade : IWorkOrdersHttpFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorkOrdersHttpFacade> _logger;

    public WorkOrdersHttpFacade(HttpClient httpClient, ILogger<WorkOrdersHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WorkOrderData?> GetWorkOrderData(int workOrderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/work-orders/{workOrderId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<WorkOrderResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null)
                {
                    return new WorkOrderData(
                        result.Id,
                        result.WorkOrderNumber,
                        result.Title,
                        result.Status,
                        result.ServiceRequestId,
                        result.Cost
                    );
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->WorkOrders] Error fetching work order {WorkOrderId}", workOrderId);
            return null;
        }
    }

    private record WorkOrderResponse(
        int Id,
        string WorkOrderNumber,
        string Title,
        string Status,
        int? ServiceRequestId,
        decimal? Cost
    );
}
