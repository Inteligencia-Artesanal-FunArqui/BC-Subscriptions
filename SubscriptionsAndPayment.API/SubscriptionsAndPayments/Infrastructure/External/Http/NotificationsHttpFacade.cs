using System.Text;
using System.Text.Json;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade implementation for communicating with the Notifications microservice.
/// </summary>
public class NotificationsHttpFacade : INotificationsHttpFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationsHttpFacade> _logger;

    public NotificationsHttpFacade(HttpClient httpClient, ILogger<NotificationsHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> CreateInAppNotification(int userId, string title, string message)
    {
        try
        {
            var requestBody = new { userId, title, message };
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v1/notifications/in-app", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Subscriptions->Notifications] In-app notification created for user {UserId}", userId);
                return true;
            }

            _logger.LogWarning("[Subscriptions->Notifications] Failed to create in-app notification: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Subscriptions->Notifications] Error creating in-app notification");
            return false;
        }
    }
}
