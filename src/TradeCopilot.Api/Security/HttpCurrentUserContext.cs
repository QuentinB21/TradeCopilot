using System.Security.Claims;
using TradeCopilot.Application.Abstractions;

namespace TradeCopilot.Api.Security;

public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private const string LocalDevelopmentUserId = "local-development-user";

    public bool IsGuest
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return string.Equals(
                user?.FindFirstValue(GuestAccessMiddleware.GuestClaimType),
                GuestAccessMiddleware.GuestClaimValue,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    public string UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirstValue("sub")
                ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

            return string.IsNullOrWhiteSpace(userId)
                ? LocalDevelopmentUserId
                : userId.Trim();
        }
    }
}
