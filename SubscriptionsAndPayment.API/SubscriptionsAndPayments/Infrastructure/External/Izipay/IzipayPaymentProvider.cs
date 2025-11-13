using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OsitoPolar.Subscriptions.Service.Domain.Services;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.External.Izipay;

/// <summary>
/// Izipay payment provider implementation
/// </summary>
public class IzipayPaymentProvider : IPaymentProvider
{
    private readonly IzipayConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Izipay";

    public IzipayPaymentProvider(IOptions<IzipayConfiguration> configuration, HttpClient httpClient)
    {
        _configuration = configuration.Value;
        _httpClient = httpClient;

        // Set base URL
        _httpClient.BaseAddress = new Uri(_configuration.ApiBaseUrl);

        // Set basic authentication
        var authToken = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_configuration.ShopId}:{_configuration.ApiKey}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", authToken);
    }

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
    {
        try
        {
            Console.WriteLine($"[Izipay] Creating payment for {request.Amount} {request.Currency}");

            // Convert amount to cents (Izipay uses smallest currency unit)
            var amountInCents = (int)(request.Amount * 100);

            var paymentData = new
            {
                amount = amountInCents,
                currency = GetIzipayCurrencyCode(request.Currency),
                orderId = request.Metadata?.GetValueOrDefault("orderId") ?? Guid.NewGuid().ToString(),
                customer = new
                {
                    email = request.CustomerEmail,
                    reference = request.CustomerEmail,
                    billingDetails = new
                    {
                        firstName = request.CustomerName.Split(' ').FirstOrDefault() ?? request.CustomerName,
                        lastName = request.CustomerName.Split(' ').Skip(1).FirstOrDefault() ?? string.Empty
                    }
                },
                formAction = "PAYMENT",
                paymentMethodToken = request.PaymentToken
            };

            var json = JsonSerializer.Serialize(paymentData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/Charge/CreatePayment", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Izipay] Error response: {responseJson}");

                return new PaymentResult
                {
                    Success = false,
                    Status = ProviderPaymentStatus.Failed,
                    ErrorMessage = $"Izipay API error: {response.StatusCode}",
                    Amount = request.Amount,
                    Currency = request.Currency
                };
            }

            var result = JsonSerializer.Deserialize<IzipayResponse>(responseJson);

            Console.WriteLine($"[Izipay] Payment created: {result?.Answer?.TransactionUuid}, Status: {result?.Status}");

            return new PaymentResult
            {
                Success = result?.Status == "SUCCESS",
                TransactionId = result?.Answer?.TransactionUuid ?? string.Empty,
                Status = MapIzipayStatus(result?.Answer?.OrderStatus),
                Amount = request.Amount,
                Currency = request.Currency,
                ErrorMessage = result?.Status != "SUCCESS"
                    ? result?.Answer?.ErrorMessage ?? "Payment failed"
                    : null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Izipay] Error: {ex.Message}");

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
            var response = await _httpClient.GetAsync($"/Transaction/Get?uuid={transactionId}");
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Izipay] Error getting status: {responseJson}");
                return ProviderPaymentStatus.Failed;
            }

            var result = JsonSerializer.Deserialize<IzipayResponse>(responseJson);
            return MapIzipayStatus(result?.Answer?.OrderStatus);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Izipay] Error getting payment status: {ex.Message}");
            return ProviderPaymentStatus.Failed;
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal? amount = null)
    {
        try
        {
            var refundData = new
            {
                uuid = transactionId,
                amount = amount.HasValue ? (int)(amount.Value * 100) : (int?)null
            };

            var json = JsonSerializer.Serialize(refundData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/Transaction/Refund", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = $"Izipay refund error: {response.StatusCode}",
                    Amount = amount ?? 0
                };
            }

            var result = JsonSerializer.Deserialize<IzipayResponse>(responseJson);

            return new RefundResult
            {
                Success = result?.Status == "SUCCESS",
                RefundId = result?.Answer?.TransactionUuid,
                Amount = amount ?? 0,
                ErrorMessage = result?.Status != "SUCCESS"
                    ? result?.Answer?.ErrorMessage
                    : null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Izipay] Error processing refund: {ex.Message}");

            return new RefundResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Amount = amount ?? 0
            };
        }
    }

    private static string GetIzipayCurrencyCode(string currency)
    {
        // Izipay uses ISO 4217 numeric codes
        return currency.ToUpper() switch
        {
            "USD" => "840",
            "PEN" => "604", // Peruvian Sol
            "EUR" => "978",
            _ => "840" // Default to USD
        };
    }

    private static ProviderPaymentStatus MapIzipayStatus(string? izipayStatus)
    {
        return izipayStatus switch
        {
            "PAID" => ProviderPaymentStatus.Succeeded,
            "RUNNING" => ProviderPaymentStatus.Processing,
            "UNPAID" => ProviderPaymentStatus.Pending,
            "CANCELLED" => ProviderPaymentStatus.Canceled,
            "ABANDONED" => ProviderPaymentStatus.Failed,
            _ => ProviderPaymentStatus.Failed
        };
    }

    // Izipay API response models
    private class IzipayResponse
    {
        public string? Status { get; set; }
        public IzipayAnswer? Answer { get; set; }
    }

    private class IzipayAnswer
    {
        public string? TransactionUuid { get; set; }
        public string? OrderStatus { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
