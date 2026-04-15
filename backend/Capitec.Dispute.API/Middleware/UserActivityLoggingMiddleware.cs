using Serilog.Context;
using System.Security.Claims;

namespace Capitec.Dispute.API.Middleware;

public class UserActivityLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public UserActivityLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userType = ResolveUserType(context);

        if (userType != null)
        {
            using (LogContext.PushProperty("UserType", userType))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }
    }

    private static string? ResolveUserType(HttpContext context)
    {
        // Authenticated requests — use JWT role
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
            return role == "Employee" ? "Employee" : "Customer";
        }

        // Unauthenticated requests — use path to determine intent
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/api/employee/", StringComparison.OrdinalIgnoreCase))
            return "Employee";

        if (path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/transactions", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/disputes", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/users", StringComparison.OrdinalIgnoreCase))
            return "Customer";

        return null; // health checks, swagger, etc. → system log
    }
}
