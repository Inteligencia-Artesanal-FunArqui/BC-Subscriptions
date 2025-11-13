namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Izipay;

/// <summary>
/// Izipay payment provider configuration
/// </summary>
public class IzipayConfiguration
{
    /// <summary>
    /// Izipay Shop ID
    /// </summary>
    public string ShopId { get; set; } = string.Empty;

    /// <summary>
    /// Izipay API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Izipay API Base URL
    /// Default: https://api.micuentaweb.pe/api-payment/V4
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.micuentaweb.pe/api-payment/V4";

    /// <summary>
    /// Public key for frontend integration
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
}
