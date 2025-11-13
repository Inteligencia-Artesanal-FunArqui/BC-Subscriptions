namespace OsitoPolar.Subscriptions.Service.Domain.Model.Commands;

public record CreatePaymentSessionCommand(int UserId, int PlanId, string SuccessUrl, string CancelUrl);