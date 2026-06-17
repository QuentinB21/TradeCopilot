using TradeCopilot.Api.Security;

namespace TradeCopilot.Api.Extensions;

public static class GuestAccessApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTradeCopilotGuestAccess(this IApplicationBuilder app) =>
        app.UseMiddleware<GuestAccessMiddleware>();
}
