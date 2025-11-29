namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade for communicating with the WorkOrders microservice.
/// </summary>
public interface IWorkOrdersHttpFacade
{
    /// <summary>
    /// Get work order data by ID
    /// </summary>
    Task<WorkOrderData?> GetWorkOrderData(int workOrderId);
}

/// <summary>
/// Work order data returned from WorkOrders microservice
/// </summary>
public record WorkOrderData(
    int Id,
    string WorkOrderNumber,
    string Title,
    string Status,
    int? ServiceRequestId,
    decimal? Cost
);
