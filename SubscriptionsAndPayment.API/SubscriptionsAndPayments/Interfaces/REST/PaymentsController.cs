using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Model.ValueObjects;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Domain.Services;
using OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;
using OsitoPolar.Subscriptions.Service.Interfaces.ACL;
using OsitoPolar.Subscriptions.Service.Shared.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;
using Stripe.Checkout;
using MassTransit;
using OsitoPolar.Shared.Events.Events;

namespace OsitoPolar.Subscriptions.Service.Interfaces.REST;

/// <summary>
/// Payment processing endpoints using Stripe.
/// Handles checkout sessions, payment verification, and plan upgrades.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Payment Processing Endpoints")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentProvider _paymentProvider;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IProfilesHttpFacade _profilesFacade;
    private readonly ISubscriptionContextFacade _subscriptionFacade;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PaymentsController> _logger;

    private const decimal PLATFORM_FEE_PERCENTAGE = 15.0m;

    public PaymentsController(
        IPaymentProvider paymentProvider,
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        IProfilesHttpFacade profilesFacade,
        ISubscriptionContextFacade subscriptionFacade,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<PaymentsController> logger)
    {
        _paymentProvider = paymentProvider;
        _paymentRepository = paymentRepository;
        _subscriptionRepository = subscriptionRepository;
        _profilesFacade = profilesFacade;
        _subscriptionFacade = subscriptionFacade;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Create Stripe Checkout Session for subscription payment
    /// </summary>
    [HttpPost("create-checkout-session")]
    [SwaggerOperation(
        Summary = "Create Stripe Checkout Session",
        Description = "Creates a Stripe Checkout Session for plan subscription",
        OperationId = "CreateCheckoutSession")]
    [SwaggerResponse(StatusCodes.Status200OK, "Checkout session created")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutSessionRequest request)
    {
        try
        {
            _logger.LogInformation("[Payments] Creating checkout session for user {UserId}, plan {PlanId}", request.UserId, request.PlanId);

            var amount = request.Amount > 0 ? request.Amount : 50.00m;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Subscription Plan #{request.PlanId}",
                                Description = "Monthly subscription plan"
                            },
                            UnitAmount = (long)(amount * 100),
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"{request.SuccessUrl}?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "userId", request.UserId.ToString() },
                    { "planId", request.PlanId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("[Payments] Checkout session created: {SessionId}", session.Id);

            return Ok(new
            {
                sessionId = session.Id,
                checkoutUrl = session.Url,
                url = session.Url,
                paymentId = session.PaymentIntentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error creating checkout session");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Verify Stripe Checkout Session status
    /// </summary>
    [HttpGet("verify/{sessionId}")]
    [SwaggerOperation(
        Summary = "Verify Checkout Session",
        Description = "Verifies the status of a Stripe Checkout Session after payment",
        OperationId = "VerifyCheckoutSession")]
    [SwaggerResponse(StatusCodes.Status200OK, "Session verified")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Session not found")]
    public async Task<IActionResult> VerifyCheckoutSession(string sessionId)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            return Ok(new
            {
                success = true,
                paymentStatus = session.PaymentStatus,
                customerEmail = session.CustomerEmail ?? session.CustomerDetails?.Email,
                amountTotal = session.AmountTotal / 100m,
                currency = session.Currency?.ToUpper(),
                metadata = session.Metadata
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error verifying session {SessionId}", sessionId);
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Complete plan upgrade after successful Stripe payment
    /// </summary>
    [HttpPost("complete-upgrade")]
    [SwaggerOperation(
        Summary = "Complete Plan Upgrade",
        Description = "Verifies Stripe payment and updates user's subscription plan",
        OperationId = "CompletePlanUpgrade")]
    [SwaggerResponse(StatusCodes.Status200OK, "Plan upgraded successfully")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Upgrade failed")]
    public async Task<IActionResult> CompletePlanUpgrade([FromBody] CompleteUpgradeRequest request)
    {
        try
        {
            _logger.LogInformation("[Payments] Processing upgrade for session: {SessionId}", request.SessionId);

            var service = new SessionService();
            var session = await service.GetAsync(request.SessionId);

            if (session.PaymentStatus != "paid")
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Payment not completed. Status: {session.PaymentStatus}"
                });
            }

            if (!session.Metadata.TryGetValue("userId", out var userIdStr) ||
                !session.Metadata.TryGetValue("planId", out var planIdStr))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Missing userId or planId in payment metadata"
                });
            }

            var userId = int.Parse(userIdStr);
            var planId = int.Parse(planIdStr);

            var subscription = await _subscriptionRepository.FindByIdAsync(planId);
            if (subscription == null)
            {
                return BadRequest(new { success = false, message = $"Plan {planId} not found" });
            }

            var payment = new Payment(
                userId,
                planId,
                subscription.Price.Amount,
                session.Id,
                session.CustomerEmail ?? session.CustomerDetails?.Email,
                $"Subscription to {subscription.PlanName}"
            );
            await _paymentRepository.AddAsync(payment);

            // Update profile plan via HTTP
            var ownerId = await _profilesFacade.FetchOwnerIdByUserId(userId);
            if (ownerId > 0)
            {
                await _profilesFacade.UpdateOwnerPlan(ownerId, planId, subscription.MaxEquipment ?? 0);
                await _unitOfWork.CompleteAsync();

                // Publish event
                var paymentEvent = new PaymentProcessedEvent
                {
                    PaymentId = payment.Id,
                    UserId = userId,
                    Amount = subscription.Price.Amount,
                    SubscriptionId = planId,
                    PaymentMethod = "Card",
                    Status = "Succeeded"
                };
                await _publishEndpoint.Publish(paymentEvent);

                _logger.LogInformation("[Payments] Owner {OwnerId} upgraded to plan {PlanId}", ownerId, planId);

                return Ok(new
                {
                    success = true,
                    message = "Plan upgraded successfully",
                    userType = "Owner",
                    planId,
                    planName = subscription.PlanName,
                    maxUnits = subscription.MaxEquipment,
                    transactionId = session.PaymentIntentId
                });
            }

            var providerId = await _profilesFacade.FetchProviderIdByUserId(userId);
            if (providerId > 0)
            {
                await _profilesFacade.UpdateProviderPlan(providerId, planId, subscription.MaxClients ?? 0);
                await _unitOfWork.CompleteAsync();

                var paymentEvent = new PaymentProcessedEvent
                {
                    PaymentId = payment.Id,
                    UserId = userId,
                    Amount = subscription.Price.Amount,
                    SubscriptionId = planId,
                    PaymentMethod = "Card",
                    Status = "Succeeded"
                };
                await _publishEndpoint.Publish(paymentEvent);

                _logger.LogInformation("[Payments] Provider {ProviderId} upgraded to plan {PlanId}", providerId, planId);

                return Ok(new
                {
                    success = true,
                    message = "Plan upgraded successfully",
                    userType = "Provider",
                    planId,
                    planName = subscription.PlanName,
                    maxClients = subscription.MaxClients,
                    transactionId = session.PaymentIntentId
                });
            }

            return BadRequest(new
            {
                success = false,
                message = $"No Owner or Provider profile found for user {userId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error completing upgrade");
            return BadRequest(new { success = false, message = $"Failed to complete upgrade: {ex.Message}" });
        }
    }

    /// <summary>
    /// Complete equipment rental after successful Stripe payment
    /// </summary>
    [HttpPost("complete-rental")]
    [SwaggerOperation(
        Summary = "Complete Equipment Rental",
        Description = "Verifies Stripe payment and assigns equipment to owner",
        OperationId = "CompleteEquipmentRental")]
    [SwaggerResponse(StatusCodes.Status200OK, "Rental completed successfully")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Rental completion failed")]
    public async Task<IActionResult> CompleteEquipmentRental([FromBody] CompleteRentalRequest request)
    {
        try
        {
            _logger.LogInformation("[Payments] Processing rental for session: {SessionId}", request.SessionId);

            var service = new SessionService();
            var session = await service.GetAsync(request.SessionId);

            if (session.PaymentStatus != "paid")
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Payment not completed. Status: {session.PaymentStatus}"
                });
            }

            if (!session.Metadata.TryGetValue("equipmentId", out var equipmentIdStr) ||
                !session.Metadata.TryGetValue("ownerId", out var ownerIdStr) ||
                !session.Metadata.TryGetValue("providerId", out var providerIdStr) ||
                !session.Metadata.TryGetValue("months", out var monthsStr) ||
                !session.Metadata.TryGetValue("monthlyFee", out var monthlyFeeStr))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Missing required metadata in payment session"
                });
            }

            var equipmentId = int.Parse(equipmentIdStr);
            var ownerId = int.Parse(ownerIdStr);
            var providerId = int.Parse(providerIdStr);
            var months = int.Parse(monthsStr);
            var monthlyFee = decimal.Parse(monthlyFeeStr);

            var totalAmount = monthlyFee * months;
            var platformFee = Math.Round(totalAmount * (PLATFORM_FEE_PERCENTAGE / 100), 2);
            var providerAmount = totalAmount - platformFee;

            var startDate = DateTimeOffset.UtcNow;
            var endDate = startDate.AddMonths(months);

            await _profilesFacade.UpdateProviderBalance(providerId, providerAmount);
            await _unitOfWork.CompleteAsync();

            var rentalEvent = new EquipmentRentalCompletedEvent
            {
                EquipmentId = equipmentId,
                EquipmentName = $"Equipment #{equipmentId}",
                EquipmentType = "Unknown",
                OwnerId = ownerId,
                ProviderId = providerId,
                RentalDurationMonths = months,
                MonthlyFee = monthlyFee,
                TotalAmount = totalAmount,
                PlatformFee = platformFee,
                ProviderAmount = providerAmount,
                RentalStartDate = startDate,
                RentalEndDate = endDate,
                StripeSessionId = session.Id
            };
            await _publishEndpoint.Publish(rentalEvent);

            _logger.LogInformation("[Payments] Rental completed for equipment {EquipmentId}", equipmentId);

            return Ok(new
            {
                success = true,
                message = $"Equipment rented successfully for {months} month(s)",
                rental = new
                {
                    equipmentId,
                    renterId = ownerId,
                    providerId,
                    rentalStartDate = startDate,
                    rentalEndDate = endDate,
                    durationMonths = months,
                    monthlyFee,
                    totalAmount,
                    platformFee,
                    providerReceived = providerAmount,
                    platformFeePercentage = PLATFORM_FEE_PERCENTAGE,
                    transactionId = session.PaymentIntentId,
                    completedAt = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error completing rental");
            return BadRequest(new { success = false, message = $"Failed to complete rental: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get payment status from Stripe
    /// </summary>
    [HttpGet("{provider}/{transactionId}")]
    [SwaggerOperation(
        Summary = "Get Payment Status",
        Description = "Check the status of a payment by provider and transaction ID",
        OperationId = "GetPaymentStatus")]
    [SwaggerResponse(StatusCodes.Status200OK, "Payment status retrieved")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Payment not found")]
    public async Task<IActionResult> GetPaymentStatus(string provider, string transactionId)
    {
        if (!provider.Equals("stripe", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only Stripe provider is currently supported" });
        }

        var status = await _paymentProvider.GetPaymentStatusAsync(transactionId);

        return Ok(new
        {
            transactionId,
            provider,
            status = status.ToString()
        });
    }

    /// <summary>
    /// Test Stripe payment processing
    /// </summary>
    [HttpPost("stripe/test")]
    [SwaggerOperation(
        Summary = "Test Stripe Payment",
        Description = "Create a test payment using Stripe provider",
        OperationId = "TestStripePayment")]
    [SwaggerResponse(StatusCodes.Status200OK, "Payment processed")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Payment failed")]
    public async Task<IActionResult> TestStripePayment([FromBody] TestPaymentRequest request)
    {
        var paymentRequest = new PaymentRequest
        {
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            PaymentToken = request.PaymentToken,
            Metadata = new Dictionary<string, string>
            {
                { "test", "true" },
                { "orderId", Guid.NewGuid().ToString() }
            }
        };

        var result = await _paymentProvider.CreatePaymentAsync(paymentRequest);

        if (result.Success)
        {
            return Ok(new
            {
                success = true,
                message = "Payment successful!",
                transactionId = result.TransactionId,
                amount = result.Amount,
                currency = result.Currency,
                status = result.Status.ToString(),
                provider = "Stripe"
            });
        }

        return BadRequest(new
        {
            success = false,
            message = result.ErrorMessage,
            status = result.Status.ToString(),
            provider = "Stripe"
        });
    }
}

// Request DTOs
public record CheckoutSessionRequest
{
    public int UserId { get; init; }
    public int PlanId { get; init; }
    public decimal Amount { get; init; } = 0;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}

public record CompleteUpgradeRequest
{
    public string SessionId { get; init; } = string.Empty;
}

public record CompleteRentalRequest
{
    public string SessionId { get; init; } = string.Empty;
}

public record TestPaymentRequest
{
    public decimal Amount { get; init; } = 50.00m;
    public string Currency { get; init; } = "USD";
    public string Description { get; init; } = "Test payment";
    public string CustomerEmail { get; init; } = "test@example.com";
    public string CustomerName { get; init; } = "Test Customer";
    public string PaymentToken { get; init; } = "pm_card_visa";
}
