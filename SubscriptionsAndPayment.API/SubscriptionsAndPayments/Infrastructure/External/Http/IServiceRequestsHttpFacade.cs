namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade for communicating with the ServiceRequests microservice.
/// </summary>
public interface IServiceRequestsHttpFacade
{
    /// <summary>
    /// Get service request data by ID
    /// </summary>
    Task<ServiceRequestData?> GetServiceRequestData(int serviceRequestId);
}

/// <summary>
/// Service request data returned from ServiceRequests microservice
/// </summary>
public record ServiceRequestData(
    int Id,
    int ClientId,
    int CompanyId,
    string Status
);
