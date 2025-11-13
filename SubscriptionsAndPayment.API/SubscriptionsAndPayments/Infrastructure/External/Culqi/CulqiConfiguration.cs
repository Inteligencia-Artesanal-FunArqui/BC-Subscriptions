namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Culqi;

/// <summary>
/// Culqi payment provider configuration
/// </summary>
public class CulqiConfiguration
{
    /// <summary>
    /// Culqi Secret Key (sk_test_... or sk_live_...)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Culqi Public Key (pk_test_... or pk_live_...)
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Culqi API Base URL
    /// Default: https://api.culqi.com/v2
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.culqi.com/v2";

    /// <summary>
    /// RSA ID for secure payload encryption (optional)
    /// </summary>
    public string? RsaId { get; set; }

    /// <summary>
    /// RSA Public Key for encryption (optional)
    /// </summary>
    public string? RsaPublicKey { get; set; }
}
