namespace OsitoPolar.Subscriptions.Service.Domain.Model.ValueObjects;

public class StripeSession
{
    public string SessionId { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    
    private StripeSession()
    {
        SessionId = string.Empty;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public StripeSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));
            
        SessionId = sessionId;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsProcessing()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Can only mark as processing from pending status");
        Status = PaymentStatus.Processing;
    }

    public void MarkAsSucceeded()
    {
        if (Status != PaymentStatus.Processing && Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Invalid status transition to succeeded");
        Status = PaymentStatus.Succeeded;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsCancelled()
    {
        Status = PaymentStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}