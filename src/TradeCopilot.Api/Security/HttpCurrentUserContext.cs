using System.Security.Claims;
using TradeCopilot.Application.Abstractions;

namespace TradeCopilot.Api.Security;

public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private const string LocalDevelopmentUserId = "local-development-user";

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
