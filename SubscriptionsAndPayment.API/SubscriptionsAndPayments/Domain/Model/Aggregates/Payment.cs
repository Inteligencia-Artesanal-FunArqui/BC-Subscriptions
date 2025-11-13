using OsitoPolar.Subscriptions.Service.Domain.Model.ValueObjects;

namespace OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;

public partial class Payment
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int SubscriptionId { get; private set; }
    public Price Amount { get; private set; }
    public StripeSession StripeSession { get; private set; }
    public string? CustomerEmail { get; private set; }
    public string? Description { get; private set; }
    
    private Payment()
    {
        Amount = new Price(0, "USD");
        StripeSession = new StripeSession("temp-session-id");
    }

    public Payment(int userId, int subscriptionId, decimal amount, string stripeSessionId, string? customerEmail = null, string? description = null)
    {
        UserId = userId;
        SubscriptionId = subscriptionId;
        Amount = new Price(amount, "USD");
        StripeSession = new StripeSession(stripeSessionId);
        CustomerEmail = customerEmail;
        Description = description;
    }

    public void UpdatePaymentStatus(PaymentStatus status)
    {
        switch (status)
        {
            case PaymentStatus.Processing:
                StripeSession.MarkAsProcessing();
                break;
            case PaymentStatus.Succeeded:
                StripeSession.MarkAsSucceeded();
                break;
            case PaymentStatus.Failed:
                StripeSession.MarkAsFailed();
                break;
            case PaymentStatus.Cancelled:
                StripeSession.MarkAsCancelled();
                break;
        }
    }
}