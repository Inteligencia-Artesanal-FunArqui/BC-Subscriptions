using OsitoPolar.Subscriptions.Service.Shared.Domain.Model;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Tokens.JWT.Services;

namespace OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Pipeline.Middleware.Components;

public class RequestAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public RequestAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        Console.WriteLine($"[Subscriptions-Middleware] Processing: {context.Request.Method} {context.Request.Path}");

        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/swagger"))
        {
            Console.WriteLine("[Subscriptions-Middleware] Skipping authorization for swagger endpoints");
            await _next(context);
            return;
        }

        try
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                Console.WriteLine($"[Subscriptions-Middleware] Token found: {token.Substring(0, Math.Min(20, token.Length))}...");

                var userId = await tokenService.ValidateToken(token);
                if (userId != null)
                {
                    Console.WriteLine($"[Subscriptions-Middleware] Token valid for userId: {userId}");
                    var user = new User(userId.Value, "", "");
                    context.Items["User"] = user;
                }
                else
                {
                    Console.WriteLine("[Subscriptions-Middleware] Token validation failed");
                }
            }
            else
            {
                Console.WriteLine("[Subscriptions-Middleware] No valid authorization header found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Subscriptions-Middleware] Error in authorization: {ex.Message}");
        }

        await _next(context);
    }
}
