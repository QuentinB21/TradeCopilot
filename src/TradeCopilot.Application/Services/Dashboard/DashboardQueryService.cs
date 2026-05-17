using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Dashboard;

namespace TradeCopilot.Application.Services.Dashboard;

public sealed class DashboardQueryService(IInvestmentRepository repository, DashboardService dashboardService) : IDashboardQueryService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        dashboardService.BuildDashboard(
            await repository.GetPortfoliosAsync(cancellationToken),
            await repository.GetAssetsAsync(cancellationToken),
            await repository.GetTransactionsAsync(cancellationToken),
            await repository.GetPricesAsync(cancellationToken),
            await repository.GetAllocationRulesAsync(cancellationToken));
}
