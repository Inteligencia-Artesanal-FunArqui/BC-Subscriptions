using Microsoft.Extensions.Options;
using OsitoPolar.Subscriptions.Service.Domain.Services;
using Stripe;
using StripeConfig = OsitoPolar.Subscriptions.Service.Infrastructure.External.Stripe.StripeConfiguration;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Stripe;

/// <summary>
/// Stripe payment provider implementation
/// </summary>
public class StripePaymentProvider : IPaymentProvider
{
    private readonly StripeConfig _configuration;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;

    public string ProviderName => "Stripe";

    public StripePaymentProvider(IOptions<StripeConfig> configuration)
    {
        _configuration = configuration.Value;

        // Set Stripe API key
        global::Stripe.StripeConfiguration.ApiKey = _configuration.SecretKey;

        // Initialize services
        _paymentIntentService = new PaymentIntentService();
        _refundService = new RefundService();
    }

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
    {
        try
        {
            Console.WriteLine($"[Stripe] Creating payment for {request.Amount} {request.Currency}");

            // Convert amount to cents (Stripe uses smallest currency unit)
            var amountInCents = (long)(request.Amount * 100);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = request.Currency.ToLower(),
                Description = request.Description,
                ReceiptEmail = request.CustomerEmail,
                PaymentMethod = request.PaymentToken, // Payment method ID from frontend
                Confirm = true, // Automatically confirm the payment
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never" // For server-side confirmation
                },
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            var paymentIntent = await _paymentIntentService.CreateAsync(options);

            Console.WriteLine($"[Stripe] Payment Intent created: {paymentIntent.Id}, Status: {paymentIntent.Status}");

            return new PaymentResult
            {
                Success = paymentIntent.Status == "succeeded",
                TransactionId = paymentIntent.Id,
                Status = MapStripeStatus(paymentIntent.Status),
                Amount = request.Amount,
                Currency = request.Currency,
                ErrorMessage = paymentIntent.Status != "succeeded"
                    ? $"Payment {paymentIntent.Status}"
                    : null
            };
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"[Stripe] Error: {ex.Message}");

            return new PaymentResult
            {
                Success = false,
                Status = ProviderPaymentStatus.Failed,
                ErrorMessage = ex.Message,
                Amount = request.Amount,
                Currency = request.Currency
            };
        }
    }

    public async Task<ProviderPaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(transactionId);
            return MapStripeStatus(paymentIntent.Status);
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"[Stripe] Error getting payment status: {ex.Message}");
            return ProviderPaymentStatus.Failed;
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal? amount = null)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = transactionId
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100); // Convert to cents
            }

            var refund = await _refundService.CreateAsync(options);

            return new RefundResult
            {
                Success = refund.Status == "succeeded",
                RefundId = refund.Id,
                Amount = refund.Amount / 100m, // Convert from cents
                ErrorMessage = refund.Status != "succeeded"
                    ? $"Refund {refund.Status}"
                    : null
            };
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"[Stripe] Error processing refund: {ex.Message}");

            return new RefundResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Amount = amount ?? 0
            };
        }
    }

    private static ProviderPaymentStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "succeeded" => ProviderPaymentStatus.Succeeded,
            "processing" => ProviderPaymentStatus.Processing,
            "requires_payment_method" => ProviderPaymentStatus.Failed,
            "requires_confirmation" => ProviderPaymentStatus.Pending,
            "requires_action" => ProviderPaymentStatus.Pending,
            "canceled" => ProviderPaymentStatus.Canceled,
            _ => ProviderPaymentStatus.Failed
        };
    }
}
