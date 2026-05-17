using TradeCopilot.Application.Contracts.Dashboard;
using TradeCopilot.Domain;

namespace TradeCopilot.Application;

public sealed class DashboardService(PositionCalculator positionCalculator)
{
    public DashboardDto BuildDashboard(
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<AssetPrice> prices,
        IReadOnlyList<AllocationRule> allocationRules)
    {
        var positions = positionCalculator.Calculate(portfolios, assets, transactions, prices, allocationRules);
        var portfolioSummaries = positions
            .GroupBy(position => new { position.PortfolioId, position.PortfolioName })
            .Select(group =>
            {
                var marketValue = group.Sum(position => position.MarketValue);
                var invested = group.Sum(position => position.InvestedAmount);
                var gain = group.Sum(position => position.UnrealizedGain);

                return new PortfolioSummaryDto(
                    group.Key.PortfolioId,
                    group.Key.PortfolioName,
                    marketValue,
                    invested,
                    gain,
                    invested > 0m ? decimal.Round(gain / invested, 4, MidpointRounding.AwayFromZero) : 0m);
            })
            .OrderBy(summary => summary.Name)
            .ToList();

        var totalMarketValue = positions.Sum(position => position.MarketValue);
        var totalInvested = positions.Sum(position => position.InvestedAmount);
        var totalGain = positions.Sum(position => position.UnrealizedGain);

        return new DashboardDto(
            totalMarketValue,
            totalInvested,
            totalGain,
            totalInvested > 0m ? decimal.Round(totalGain / totalInvested, 4, MidpointRounding.AwayFromZero) : 0m,
            portfolioSummaries,
            positions.OrderBy(position => position.PortfolioName).ThenByDescending(position => position.MarketValue).ToList());
    }
}
