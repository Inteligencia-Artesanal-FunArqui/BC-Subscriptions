namespace OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;

/// <summary>
/// Represents a payment for a completed service
/// Owner pays Provider for service work
/// </summary>
public class ServicePayment
{
    public int Id { get; private set; }

    /// <summary>
    /// Reference to completed WorkOrder
    /// </summary>
    public int WorkOrderId { get; private set; }

    /// <summary>
    /// Reference to ServiceRequest
    /// </summary>
    public int ServiceRequestId { get; private set; }

    /// <summary>
    /// Owner who pays
    /// </summary>
    public int OwnerId { get; private set; }

    /// <summary>
    /// Provider who receives payment
    /// </summary>
    public int ProviderId { get; private set; }

    /// <summary>
    /// Total amount charged to Owner
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Platform commission (e.g., 15%)
    /// </summary>
    public decimal PlatformFee { get; private set; }

    /// <summary>
    /// Amount received by Provider (TotalAmount - PlatformFee)
    /// </summary>
    public decimal ProviderAmount { get; private set; }

    /// <summary>
    /// Stripe payment intent ID
    /// </summary>
    public string? StripePaymentIntentId { get; private set; }

    /// <summary>
    /// Stripe transaction ID
    /// </summary>
    public string? StripeTransactionId { get; private set; }

    /// <summary>
    /// Payment status: Pending, Completed, Failed, Refunded
    /// </summary>
    public string Status { get; private set; }

    /// <summary>
    /// When payment was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When payment was completed
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Payment description
    /// </summary>
    public string Description { get; private set; }

    protected ServicePayment()
    {
        Status = "Pending";
        CreatedAt = DateTime.UtcNow;
        Description = string.Empty;
    }

    public ServicePayment(
        int workOrderId,
        int serviceRequestId,
        int ownerId,
        int providerId,
        decimal totalAmount,
        decimal platformFeePercentage,
        string description)
    {
        WorkOrderId = workOrderId;
        ServiceRequestId = serviceRequestId;
        OwnerId = ownerId;
        ProviderId = providerId;
        TotalAmount = totalAmount;

        // Calculate platform fee and provider amount
        PlatformFee = Math.Round(totalAmount * (platformFeePercentage / 100), 2);
        ProviderAmount = totalAmount - PlatformFee;

        Status = "Pending";
        CreatedAt = DateTime.UtcNow;
        Description = description;
    }

    public void MarkAsCompleted(string stripePaymentIntentId, string stripeTransactionId)
    {
        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
        StripePaymentIntentId = stripePaymentIntentId;
        StripeTransactionId = stripeTransactionId;
    }

    public void MarkAsFailed()
    {
        Status = "Failed";
    }

    public void MarkAsRefunded()
    {
        Status = "Refunded";
    }
}
