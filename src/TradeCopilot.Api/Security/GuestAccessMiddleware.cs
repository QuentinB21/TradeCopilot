using System.Security.Claims;

namespace TradeCopilot.Api.Security;

public sealed class GuestAccessMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-TradeCopilot-Guest";
    public const string GuestUserId = "guest-demo";
    public const string GuestClaimType = "tradecopilot:mode";
    public const string GuestClaimValue = "guest";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true || !IsGuestRequest(context))
        {
            await next(context);
            return;
        }

        if (!IsGuestSafeRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Le mode invite est disponible en lecture seule.");
            return;
        }

        var identity = new ClaimsIdentity(
            [
                new Claim("sub", GuestUserId),
                new Claim(ClaimTypes.NameIdentifier, GuestUserId),
                new Claim(ClaimTypes.Name, "Invite"),
                new Claim(GuestClaimType, GuestClaimValue)
            ],
            authenticationType: "TradeCopilotGuest");

        context.User = new ClaimsPrincipal(identity);
        await next(context);
    }

    private static bool IsGuestRequest(HttpContext context) =>
        context.Request.Headers.TryGetValue(HeaderName, out var values)
        && values.Any(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

    private static bool IsGuestSafeRequest(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method))
        {
            return true;
        }

        return HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals("/api/monthly-plan", StringComparison.OrdinalIgnoreCase);
    }
}
