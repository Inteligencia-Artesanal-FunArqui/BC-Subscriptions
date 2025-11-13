using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Model.Commands;
using OsitoPolar.Subscriptions.Service.Domain.Model.ValueObjects;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Domain.Services;
using OsitoPolar.Subscriptions.Service.Shared.Domain.Repositories;

namespace OsitoPolar.Subscriptions.Service.Application.Internal.CommandServices;

public class PaymentCommandService : IPaymentCommandService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionCommandService _subscriptionCommandService;
    private readonly IStripeService _stripeService;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentCommandService(
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionCommandService subscriptionCommandService,
        IStripeService stripeService,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _subscriptionRepository = subscriptionRepository;
        _subscriptionCommandService = subscriptionCommandService;
        _stripeService = stripeService;
        _unitOfWork = unitOfWork;
    }

    public async Task<(Payment payment, string checkoutUrl)> Handle(CreatePaymentSessionCommand command)
    {
        // Get the plan details
        var plan = await _subscriptionRepository.FindByIdAsync(command.PlanId)
            ?? throw new InvalidOperationException($"Plan with ID {command.PlanId} not found");

        // Create Stripe checkout session
        var successUrlWithSession = command.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}";
        var checkoutUrl = await _stripeService.CreateCheckoutSessionAsync(
            command.PlanId,
            successUrlWithSession,
            command.CancelUrl
        );

        // Extract session ID from URL (Stripe returns the full URL)
        var sessionId = ExtractSessionIdFromUrl(checkoutUrl);

        // Create payment record
        var payment = new Payment(
            command.UserId,
            command.PlanId,
            plan.Price.Amount,
            sessionId,
            null,
            $"Subscription to {plan.PlanName}"
        );

        await _paymentRepository.AddAsync(payment);
        await _unitOfWork.CompleteAsync();

        return (payment, checkoutUrl);
    }

    public async Task<Payment?> Handle(ProcessPaymentWebhookCommand command)
    {
        var payment = await _paymentRepository.FindByStripeSessionIdAsync(command.StripeSessionId);
        
        if (payment == null)
            return null;

        // Update payment status based on webhook event
        var status = command.EventType switch
        {
            "checkout.session.completed" => PaymentStatus.Succeeded,
            "checkout.session.expired" => PaymentStatus.Cancelled,
            "payment_intent.payment_failed" => PaymentStatus.Failed,
            _ => payment.StripeSession.Status
        };

        payment.UpdatePaymentStatus(status);

        // If payment succeeded, upgrade the user's subscription
        if (status == PaymentStatus.Succeeded)
        {
            var upgradePlanCommand = new UpgradePlanCommand(payment.UserId, payment.SubscriptionId);
            await _subscriptionCommandService.Handle(upgradePlanCommand);
        }

        _paymentRepository.Update(payment);
        await _unitOfWork.CompleteAsync();

        return payment;
    }

    private string ExtractSessionIdFromUrl(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Split('/');
        return segments.LastOrDefault() ?? url;
    }
}