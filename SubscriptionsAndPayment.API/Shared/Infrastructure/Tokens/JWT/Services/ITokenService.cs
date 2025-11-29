namespace OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Tokens.JWT.Services;

public interface ITokenService
{
    Task<int?> ValidateToken(string token);
}
