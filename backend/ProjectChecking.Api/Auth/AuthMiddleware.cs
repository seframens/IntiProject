using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ProjectChecking.Api.Models;

namespace ProjectChecking.Api.Auth;

public static class HttpContextExtensions
{
    private const string SessionKey = "session";

    public static Session? GetSession(this HttpContext context)
    {
        return context.Items.TryGetValue(SessionKey, out var value) ? value as Session : null;
    }

    public static void SetSession(this HttpContext context, Session session)
    {
        context.Items[SessionKey] = session;
    }
}

public class AuthMiddleware
{
    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ISessionStore sessions)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var header))
        {
            var value = header.ToString();
            if (value.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
            {
                var token = value.Substring("Bearer ".Length).Trim();
                var session = sessions.Get(token);
                if (session != null)
                {
                    context.SetSession(session);
                }
            }
        }
        await _next(context);
    }
}

public class RequireAuthFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context,
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>();
        if (allowAnonymous is null && context.HttpContext.GetSession() is null)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
            return;
        }
        await next();
    }
}
