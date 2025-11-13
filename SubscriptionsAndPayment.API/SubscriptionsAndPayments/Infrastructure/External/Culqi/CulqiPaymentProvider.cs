using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OsitoPolar.Subscriptions.Service.Domain.Services;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Culqi;

/// <summary>
/// Culqi payment provider implementation
/// Supports: Cards, Yape, Plin, Payment Links (CulqiLink), QR codes
/// </summary>
public class CulqiPaymentProvider : IPaymentProvider
{
    private readonly CulqiConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Culqi";

    public CulqiPaymentProvider(IOptions<CulqiConfiguration> configuration, HttpClient httpClient)
    {
        _configuration = configuration.Value;
        _httpClient = httpClient;

        // Set base URL
        _httpClient.BaseAddress = new Uri(_configuration.ApiBaseUrl);

        // Set authorization header (Bearer token with Secret Key)
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _configuration.SecretKey);
    }

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
    {
        try
        {
            Console.WriteLine($"[Culqi] Creating payment for {request.Amount} {request.Currency}");

            // Convert amount to cents (Culqi uses smallest currency unit)
            var amountInCents = (int)(request.Amount * 100);

            var chargeData = new
            {
                amount = amountInCents,
                currency_code = GetCulqiCurrencyCode(request.Currency),
                description = request.Description,
                email = request.CustomerEmail,
                source_id = request.PaymentToken, // Token from Culqi.js or mobile SDK
                metadata = request.Metadata ?? new Dictionary<string, string>
                {
                    { "customer_name", request.CustomerName }
                }
            };

            var json = JsonSerializer.Serialize(chargeData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/charges", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Culqi] Error response: {responseJson}");

                var errorResponse = JsonSerializer.Deserialize<CulqiErrorResponse>(responseJson);

                return new PaymentResult
                {
                    Success = false,
                    Status = ProviderPaymentStatus.Failed,
                    ErrorMessage = errorResponse?.UserMessage ?? $"Culqi API error: {response.StatusCode}",
                    Amount = request.Amount,
                    Currency = request.Currency
                };
            }

            var result = JsonSerializer.Deserialize<CulqiChargeResponse>(responseJson);

            Console.WriteLine($"[Culqi] Charge created: {result?.Id}, Status: {result?.Outcome?.Type}");

            return new PaymentResult
            {
                Success = result?.Outcome?.Type == "venta_exitosa",
                TransactionId = result?.Id ?? string.Empty,
                Status = MapCulqiStatus(result?.Outcome?.Type),
                Amount = request.Amount,
                Currency = request.Currency,
                ErrorMessage = result?.Outcome?.Type != "venta_exitosa"
                    ? result?.Outcome?.UserMessage ?? "Payment failed"
                    : null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Culqi] Error: {ex.Message}");

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
            var response = await _httpClient.GetAsync($"/charges/{transactionId}");
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Culqi] Error getting status: {responseJson}");
                return ProviderPaymentStatus.Failed;
            }

            var result = JsonSerializer.Deserialize<CulqiChargeResponse>(responseJson);
            return MapCulqiStatus(result?.Outcome?.Type);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Culqi] Error getting payment status: {ex.Message}");
            return ProviderPaymentStatus.Failed;
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal? amount = null)
    {
        try
        {
            var refundData = new
            {
                amount = amount.HasValue ? (int)(amount.Value * 100) : (int?)null,
                charge_id = transactionId,
                reason = "solicitud_comprador" // Culqi refund reason
            };

            var json = JsonSerializer.Serialize(refundData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/refunds", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<CulqiErrorResponse>(responseJson);

                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = errorResponse?.UserMessage ?? $"Culqi refund error: {response.StatusCode}",
                    Amount = amount ?? 0
                };
            }

            var result = JsonSerializer.Deserialize<CulqiRefundResponse>(responseJson);

            return new RefundResult
            {
                Success = result?.Object == "refund",
                RefundId = result?.Id,
                Amount = result?.Amount / 100m ?? 0, // Convert from cents
                ErrorMessage = result?.Object != "refund"
                    ? "Refund failed"
                    : null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Culqi] Error processing refund: {ex.Message}");

            return new RefundResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Amount = amount ?? 0
            };
        }
    }

    private static string GetCulqiCurrencyCode(string currency)
    {
        // Culqi uses ISO 4217 alpha codes
        return currency.ToUpper() switch
        {
            "USD" => "USD",
            "PEN" => "PEN", // Peruvian Sol
            "EUR" => "EUR",
            _ => "PEN" // Default to PEN for Peru
        };
    }

    private static ProviderPaymentStatus MapCulqiStatus(string? culqiStatus)
    {
        return culqiStatus switch
        {
            "venta_exitosa" => ProviderPaymentStatus.Succeeded,
            "pending" => ProviderPaymentStatus.Processing,
            "rechazada" => ProviderPaymentStatus.Failed,
            "cancelada" => ProviderPaymentStatus.Canceled,
            _ => ProviderPaymentStatus.Failed
        };
    }

    // Culqi API response models
    private class CulqiChargeResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public int Amount { get; set; }
        public string? CurrencyCode { get; set; }
        public string? Email { get; set; }
        public CulqiOutcome? Outcome { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    private class CulqiOutcome
    {
        public string? Type { get; set; }
        public string? MerchantMessage { get; set; }
        public string? UserMessage { get; set; }
    }

    private class CulqiErrorResponse
    {
        public string? Object { get; set; }
        public string? Type { get; set; }
        public string? MerchantMessage { get; set; }
        public string? UserMessage { get; set; }
    }

    private class CulqiRefundResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public int Amount { get; set; }
        public string? ChargeId { get; set; }
    }
}
