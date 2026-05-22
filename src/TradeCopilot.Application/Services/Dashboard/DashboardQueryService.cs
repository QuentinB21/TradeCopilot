using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Dashboard;
using TradeCopilot.Application.Services.Prices;

namespace TradeCopilot.Application.Services.Dashboard;

public sealed class DashboardQueryService(
    IInvestmentRepository repository,
    DashboardService dashboardService,
    IMarketPriceRefreshService marketPriceRefreshService) : IDashboardQueryService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var portfolios = await repository.GetPortfoliosAsync(cancellationToken);
        var assets = await repository.GetAssetsAsync(cancellationToken);
        var transactions = await repository.GetTransactionsAsync(cancellationToken);
        var prices = await repository.GetPricesAsync(cancellationToken);
        var repartitions = await repository.GetAssetRepartitionsAsync(cancellationToken);
        var refreshedPrices = await marketPriceRefreshService.RefreshCurrentPricesAsync(assets, transactions, prices, cancellationToken);

        return dashboardService.BuildDashboard(portfolios, assets, transactions, refreshedPrices, repartitions);
    }
}
