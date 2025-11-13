namespace OsitoPolar.Subscriptions.Service.Domain.Model.Commands;

public record ProcessPaymentWebhookCommand(string StripeSessionId, string EventType);