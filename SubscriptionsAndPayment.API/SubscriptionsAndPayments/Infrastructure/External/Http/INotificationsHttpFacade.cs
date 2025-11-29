namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade for communicating with the Notifications microservice.
/// </summary>
public interface INotificationsHttpFacade
{
    /// <summary>
    /// Create an in-app notification
    /// </summary>
    Task<bool> CreateInAppNotification(int userId, string title, string message);
}
