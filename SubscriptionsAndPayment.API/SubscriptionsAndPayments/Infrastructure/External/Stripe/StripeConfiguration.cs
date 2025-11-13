namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Stripe;

/// <summary>
/// Stripe payment provider configuration
/// </summary>
public class StripeConfiguration
{
    /// <summary>
    /// Stripe Secret Key (sk_test_... or sk_live_...)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe Publishable Key (pk_test_... or pk_live_...)
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook signing secret (whsec_...)
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
