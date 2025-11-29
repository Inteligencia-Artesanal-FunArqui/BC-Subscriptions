using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;
using OsitoPolar.Subscriptions.Service.Shared.Domain.Repositories;
using Stripe;
using Stripe.Checkout;
using MassTransit;
using OsitoPolar.Shared.Events.Events;

namespace OsitoPolar.Subscriptions.Service.Interfaces.REST;

/// <summary>
/// Controller for service payments (Owner pays Provider after service completion)
/// </summary>
[ApiController]
[Route("api/v1/service-payments")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Service Payments (Owner → Provider)")]
public class ServicePaymentsController : ControllerBase
{
    private readonly IWorkOrdersHttpFacade _workOrdersFacade;
    private readonly IServiceRequestsHttpFacade _serviceRequestsFacade;
    private readonly IProfilesHttpFacade _profilesFacade;
    private readonly INotificationsHttpFacade _notificationsFacade;
    private readonly IServicePaymentRepository _servicePaymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ServicePaymentsController> _logger;

    // Platform commission percentage
    private const decimal PLATFORM_FEE_PERCENTAGE = 15.0m; // 15%

    public ServicePaymentsController(
        IWorkOrdersHttpFacade workOrdersFacade,
        IServiceRequestsHttpFacade serviceRequestsFacade,
        IProfilesHttpFacade profilesFacade,
        INotificationsHttpFacade notificationsFacade,
        IServicePaymentRepository servicePaymentRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<ServicePaymentsController> logger)
    {
        _workOrdersFacade = workOrdersFacade;
        _serviceRequestsFacade = serviceRequestsFacade;
        _profilesFacade = profilesFacade;
        _notificationsFacade = notificationsFacade;
        _servicePaymentRepository = servicePaymentRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Create payment intent for completed service (Owner pays)
    /// </summary>
    [HttpPost("create-checkout")]
    [SwaggerOperation(
        Summary = "Create Service Payment Checkout",
        Description = "Owner creates payment for completed service. Returns Stripe checkout URL.",
        OperationId = "CreateServicePaymentCheckout")]
    [SwaggerResponse(StatusCodes.Status200OK, "Checkout session created")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request or work order not resolved")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Only owners can pay for services")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Work order not found")]
    public async Task<IActionResult> CreateServicePaymentCheckout([FromBody] CreateServicePaymentResource resource)
    {
        try
        {
            // Verify user is an Owner
            var ownerId = await _profilesFacade.FetchOwnerIdByUserId(resource.UserId);
            if (ownerId == 0)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Only owners can pay for services" });

            _logger.LogInformation("Owner {OwnerId} creating payment for work order {WorkOrderId}",
                ownerId, resource.WorkOrderId);

            // Get work order
            var workOrderData = await _workOrdersFacade.GetWorkOrderData(resource.WorkOrderId);
            if (workOrderData == null)
                return NotFound(new { message = "Work order not found" });

            // Verify work order is resolved and has a cost
            if (workOrderData.Status != "Resolved")
                return BadRequest(new { message = "Work order must be resolved before payment" });

            if (!workOrderData.Cost.HasValue || workOrderData.Cost.Value <= 0)
                return BadRequest(new { message = "Work order must have a valid cost" });

            // Get service request
            if (!workOrderData.ServiceRequestId.HasValue)
                return NotFound(new { message = "Service request not found for work order" });

            var serviceRequestData = await _serviceRequestsFacade.GetServiceRequestData(workOrderData.ServiceRequestId.Value);
            if (serviceRequestData == null)
                return NotFound(new { message = "Service request not found" });

            // Verify owner owns this service request
            if (serviceRequestData.ClientId != ownerId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "You can only pay for your own service requests" });

            // Get provider company name
            var providerCompanyName = await _profilesFacade.FetchProviderCompanyName(serviceRequestData.CompanyId);
            if (string.IsNullOrEmpty(providerCompanyName))
                return NotFound(new { message = "Provider not found" });

            // Calculate amounts
            var totalAmount = workOrderData.Cost.Value;
            var platformFee = Math.Round(totalAmount * (PLATFORM_FEE_PERCENTAGE / 100), 2);
            var providerAmount = totalAmount - platformFee;

            _logger.LogInformation(
                "Payment breakdown - Total: ${Total}, Platform Fee: ${Fee} ({Percentage}%), Provider Gets: ${Provider}",
                totalAmount, platformFee, PLATFORM_FEE_PERCENTAGE, providerAmount);

            // Create ServicePayment record
            var servicePayment = new ServicePayment(
                workOrderData.Id,
                serviceRequestData.Id,
                ownerId,
                serviceRequestData.CompanyId,
                totalAmount,
                PLATFORM_FEE_PERCENTAGE,
                $"Service payment for Work Order #{workOrderData.WorkOrderNumber}");

            await _servicePaymentRepository.AddAsync(servicePayment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("ServicePayment record created with ID: {PaymentId}", servicePayment.Id);

            // Create Stripe Checkout Session
            var successUrl = resource.SuccessUrl ?? "http://localhost:5173/payments/success";
            var cancelUrl = resource.CancelUrl ?? "http://localhost:5173/payments/cancel";

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
                                Name = $"Service Payment: {workOrderData.Title}",
                                Description = $"Work Order #{workOrderData.WorkOrderNumber} - {providerCompanyName}"
                            },
                            UnitAmount = (long)(totalAmount * 100), // Convert to cents
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"{successUrl}?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "paymentType", "service" },
                    { "servicePaymentId", servicePayment.Id.ToString() },
                    { "workOrderId", workOrderData.Id.ToString() },
                    { "serviceRequestId", serviceRequestData.Id.ToString() },
                    { "ownerId", ownerId.ToString() },
                    { "providerId", serviceRequestData.CompanyId.ToString() },
                    { "totalAmount", totalAmount.ToString("F2") },
                    { "platformFee", platformFee.ToString("F2") },
                    { "providerAmount", providerAmount.ToString("F2") }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Stripe checkout session created: {SessionId}", session.Id);

            return Ok(new
            {
                checkoutUrl = session.Url,
                sessionId = session.Id,
                totalAmount,
                platformFee,
                providerAmount,
                platformFeePercentage = PLATFORM_FEE_PERCENTAGE,
                workOrder = new
                {
                    id = workOrderData.Id,
                    workOrderNumber = workOrderData.WorkOrderNumber,
                    title = workOrderData.Title,
                    providerName = providerCompanyName
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service payment checkout");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Complete service payment after successful Stripe payment
    /// </summary>
    [HttpPost("complete")]
    [SwaggerOperation(
        Summary = "Complete Service Payment",
        Description = "Completes service payment after Stripe checkout success",
        OperationId = "CompleteServicePayment")]
    [SwaggerResponse(StatusCodes.Status200OK, "Payment completed")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Payment completion failed")]
    public async Task<IActionResult> CompleteServicePayment([FromBody] CompleteServicePaymentRequest request)
    {
        try
        {
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

            if (!session.Metadata.TryGetValue("paymentType", out var paymentType) || paymentType != "service")
            {
                return BadRequest(new { success = false, message = "Invalid payment type" });
            }

            var servicePaymentId = int.Parse(session.Metadata["servicePaymentId"]);
            var workOrderId = int.Parse(session.Metadata["workOrderId"]);
            var providerId = int.Parse(session.Metadata["providerId"]);
            var providerAmount = decimal.Parse(session.Metadata["providerAmount"]);

            _logger.LogInformation(
                "Processing service payment completion - ServicePayment: {ServicePaymentId}, WorkOrder: {WorkOrderId}, Provider: {ProviderId}, Amount: ${Amount}",
                servicePaymentId, workOrderId, providerId, providerAmount);

            // Update ServicePayment record
            var servicePayment = await _servicePaymentRepository.FindByIdAsync(servicePaymentId);
            if (servicePayment != null)
            {
                servicePayment.MarkAsCompleted(
                    session.PaymentIntentId ?? session.Id,
                    session.Id);
                _servicePaymentRepository.Update(servicePayment);

                _logger.LogInformation("ServicePayment {PaymentId} marked as completed", servicePaymentId);
            }

            // Update provider balance
            var balanceUpdated = await _profilesFacade.UpdateProviderBalance(providerId, providerAmount);
            if (balanceUpdated)
            {
                _logger.LogInformation("Provider {ProviderId} balance updated with ${Amount}", providerId, providerAmount);
            }

            // Save all changes
            await _unitOfWork.CompleteAsync();

            // Publish event
            var paymentEvent = new ServicePaymentCompletedEvent
            {
                ServicePaymentId = servicePaymentId,
                WorkOrderId = workOrderId,
                OwnerId = int.Parse(session.Metadata["ownerId"]),
                ProviderId = providerId,
                TotalAmount = decimal.Parse(session.Metadata["totalAmount"]),
                PlatformFee = decimal.Parse(session.Metadata["platformFee"]),
                ProviderAmount = providerAmount,
                StripeSessionId = session.Id
            };
            await _publishEndpoint.Publish(paymentEvent);

            // Generate notification for provider
            var workOrderData = await _workOrdersFacade.GetWorkOrderData(workOrderId);
            if (workOrderData != null)
            {
                var providerUserId = await _profilesFacade.GetProviderUserIdByProviderId(providerId);
                if (providerUserId > 0)
                {
                    var message = $"Payment of ${providerAmount:F2} received for service: {workOrderData.Title}";
                    await _notificationsFacade.CreateInAppNotification(
                        providerUserId,
                        "Payment Received",
                        message);

                    _logger.LogInformation("Payment notification sent to provider {ProviderId}", providerId);
                }
            }

            return Ok(new
            {
                success = true,
                message = "Payment completed successfully",
                servicePaymentId,
                workOrderId,
                providerId,
                providerAmount,
                transactionId = session.PaymentIntentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing service payment");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get service payment by work order ID
    /// </summary>
    [HttpGet("by-work-order/{workOrderId:int}")]
    [SwaggerOperation(
        Summary = "Get Service Payment by Work Order",
        Description = "Returns the service payment for a specific work order",
        OperationId = "GetServicePaymentByWorkOrder")]
    [SwaggerResponse(StatusCodes.Status200OK, "Payment found")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Payment not found")]
    public async Task<IActionResult> GetServicePaymentByWorkOrder(int workOrderId)
    {
        try
        {
            var payment = await _servicePaymentRepository.FindByWorkOrderIdAsync(workOrderId);
            if (payment == null)
                return NotFound(new { message = "Payment not found for this work order" });

            return Ok(new
            {
                paymentId = payment.Id,
                workOrderId = payment.WorkOrderId,
                serviceRequestId = payment.ServiceRequestId,
                ownerId = payment.OwnerId,
                providerId = payment.ProviderId,
                totalAmount = payment.TotalAmount,
                platformFee = payment.PlatformFee,
                providerAmount = payment.ProviderAmount,
                status = payment.Status,
                description = payment.Description,
                createdAt = payment.CreatedAt,
                completedAt = payment.CompletedAt,
                stripePaymentIntentId = payment.StripePaymentIntentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service payment");
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Resource for creating service payment
/// </summary>
public record CreateServicePaymentResource
{
    public int UserId { get; init; }
    public int WorkOrderId { get; init; }
    public string? SuccessUrl { get; init; }
    public string? CancelUrl { get; init; }
}

/// <summary>
/// Request for completing service payment
/// </summary>
public record CompleteServicePaymentRequest
{
    public string SessionId { get; init; } = string.Empty;
}
