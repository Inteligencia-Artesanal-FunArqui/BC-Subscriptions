namespace OsitoPolar.Subscriptions.Service.Domain.Model.ValueObjects;

public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5
}