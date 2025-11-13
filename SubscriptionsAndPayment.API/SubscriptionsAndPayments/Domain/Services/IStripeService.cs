namespace OsitoPolar.Subscriptions.Service.Domain.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(int planId, string successUrl, string cancelUrl, string? customerEmail = null);
    Task<bool> ValidateWebhookSignatureAsync(string payload, string signature);
    Task<(string sessionId, string status, string? customerEmail)> GetSessionDetailsAsync(string sessionId);
}