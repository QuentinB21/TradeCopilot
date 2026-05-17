using TradeCopilot.Application.Contracts.Dashboard;

namespace TradeCopilot.Application.Services.Dashboard;

public interface IDashboardQueryService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
