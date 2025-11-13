namespace OsitoPolar.Subscriptions.Service.Domain.Services;

/// <summary>
/// Payment provider interface
/// Implementations: Stripe, Izipay, PayPal
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Provider name (e.g., "Stripe", "Izipay", "PayPal")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Create a payment intent/charge
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <returns>Payment result with transaction ID</returns>
    Task<PaymentResult> CreatePaymentAsync(PaymentRequest request);

    /// <summary>
    /// Verify payment status
    /// </summary>
    /// <param name="transactionId">Transaction ID from payment provider</param>
    /// <returns>Payment status</returns>
    Task<ProviderPaymentStatus> GetPaymentStatusAsync(string transactionId);

    /// <summary>
    /// Process refund
    /// </summary>
    /// <param name="transactionId">Original transaction ID</param>
    /// <param name="amount">Amount to refund (null = full refund)</param>
    /// <returns>Refund result</returns>
    Task<RefundResult> RefundPaymentAsync(string transactionId, decimal? amount = null);
}

/// <summary>
/// Payment request details
/// </summary>
public record PaymentRequest
{
    /// <summary>
    /// Amount to charge (in USD)
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Currency code (default: USD)
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Payment description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Customer email
    /// </summary>
    public required string CustomerEmail { get; init; }

    /// <summary>
    /// Customer name
    /// </summary>
    public required string CustomerName { get; init; }

    /// <summary>
    /// Payment token from frontend (credit card token, PayPal token, etc.)
    /// </summary>
    public required string PaymentToken { get; init; }

    /// <summary>
    /// Metadata for tracking (order ID, user ID, etc.)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Payment result
/// </summary>
public record PaymentResult
{
    /// <summary>
    /// Was payment successful?
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Transaction ID from payment provider
    /// </summary>
    public string? TransactionId { get; init; }

    /// <summary>
    /// Payment status
    /// </summary>
    public required ProviderPaymentStatus Status { get; init; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Amount charged
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; init; } = "USD";
}

/// <summary>
/// Payment provider status enum
/// </summary>
public enum ProviderPaymentStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Canceled,
    Refunded
}

/// <summary>
/// Refund result
/// </summary>
public record RefundResult
{
    /// <summary>
    /// Was refund successful?
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Refund ID
    /// </summary>
    public string? RefundId { get; init; }

    /// <summary>
    /// Amount refunded
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; init; }
}
