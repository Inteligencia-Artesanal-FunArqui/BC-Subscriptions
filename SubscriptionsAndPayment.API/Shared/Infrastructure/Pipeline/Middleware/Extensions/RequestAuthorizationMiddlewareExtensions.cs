using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Pipeline.Middleware.Components;

namespace OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Pipeline.Middleware.Extensions;

public static class RequestAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestAuthorizationMiddleware>();
    }
}
