using OsitoPolar.Subscriptions.Service.Shared.Domain.Model.Events;

namespace OsitoPolar.Subscriptions.Service.Domain.Model.Events;

/// <summary>
/// Domain event raised when a payment is successfully processed (subscription or service payment)
/// </summary>
public record PaymentProcessedEvent : IEvent
{
    public int PaymentId { get; init; }
    public string PaymentType { get; init; } // "Subscription" or "Service"
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public int? SubscriptionId { get; init; } // For subscription payments
    public int? ServicePaymentId { get; init; } // For service payments
    public int? ProviderId { get; init; } // For service payments - who receives the money
    public decimal? ProviderAmount { get; init; } // For service payments - amount provider receives
    public string StripeSessionId { get; init; }
    public DateTime OccurredAt { get; init; }

    public PaymentProcessedEvent(
        int paymentId,
        string paymentType,
        int userId,
        decimal amount,
        string stripeSessionId,
        int? subscriptionId = null,
        int? servicePaymentId = null,
        int? providerId = null,
        decimal? providerAmount = null)
    {
        PaymentId = paymentId;
        PaymentType = paymentType ?? throw new ArgumentNullException(nameof(paymentType));
        UserId = userId;
        Amount = amount;
        StripeSessionId = stripeSessionId ?? throw new ArgumentNullException(nameof(stripeSessionId));
        SubscriptionId = subscriptionId;
        ServicePaymentId = servicePaymentId;
        ProviderId = providerId;
        ProviderAmount = providerAmount;
        OccurredAt = DateTime.UtcNow;
    }
}
