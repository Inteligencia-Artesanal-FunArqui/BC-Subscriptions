using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Model.Commands;

namespace OsitoPolar.Subscriptions.Service.Domain.Services;

public interface IPaymentCommandService
{
    Task<(Payment payment, string checkoutUrl)> Handle(CreatePaymentSessionCommand command);
    Task<Payment?> Handle(ProcessPaymentWebhookCommand command);
}