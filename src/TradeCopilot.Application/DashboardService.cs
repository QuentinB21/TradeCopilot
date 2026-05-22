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
        IReadOnlyList<Repartition> repartitions)
    {
        var positions = positionCalculator.Calculate(portfolios, assets, transactions, prices, repartitions);
        var totalMarketValue = positions.Sum(position => position.MarketValue);
        var totalInvested = positions.Sum(position => position.InvestedAmount);
        var totalGain = positions.Sum(position => position.UnrealizedGain);
        var portfolioSummaries = BuildPortfolioSummaries(portfolios, positions, totalMarketValue);
        var history = BuildHistory(portfolios, assets, transactions, prices, repartitions);

        return new DashboardDto(
            totalMarketValue,
            totalInvested,
            totalGain,
            totalInvested > 0m ? decimal.Round(totalGain / totalInvested, 4, MidpointRounding.AwayFromZero) : 0m,
            portfolioSummaries,
            positions.OrderBy(position => position.PortfolioName).ThenByDescending(position => position.MarketValue).ToList(),
            history);
    }

    private static IReadOnlyList<PortfolioSummaryDto> BuildPortfolioSummaries(
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Contracts.Positions.PositionDto> positions,
        decimal totalMarketValue)
    {
        var positionsByPortfolio = positions
            .GroupBy(position => position.PortfolioId)
            .ToDictionary(group => group.Key);

        return portfolios
            .OrderBy(portfolio => portfolio.Name)
            .Select(portfolio =>
            {
                positionsByPortfolio.TryGetValue(portfolio.Id, out var portfolioPositions);
                var marketValue = portfolioPositions?.Sum(position => position.MarketValue) ?? 0m;
                var invested = portfolioPositions?.Sum(position => position.InvestedAmount) ?? 0m;
                var gain = portfolioPositions?.Sum(position => position.UnrealizedGain) ?? 0m;
                var actualWeight = totalMarketValue > 0m ? Round(marketValue / totalMarketValue) : 0m;
                var targetWeight = Round(portfolio.TargetWeight);

                return new PortfolioSummaryDto(
                    portfolio.Id,
                    portfolio.Name,
                    Round(marketValue),
                    Round(invested),
                    Round(gain),
                    invested > 0m ? Round(gain / invested) : 0m,
                    targetWeight,
                    actualWeight,
                    targetWeight > 0m ? Round(actualWeight - targetWeight) : 0m);
            })
            .ToList();
    }

    private IReadOnlyList<DashboardHistoryPointDto> BuildHistory(
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<AssetPrice> prices,
        IReadOnlyList<Repartition> repartitions)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dates = transactions
            .Select(transaction => transaction.Date)
            .Concat(prices.Select(price => price.Date))
            .Append(today)
            .Distinct()
            .OrderBy(date => date)
            .ToList();

        if (dates.Count > 120)
        {
            dates = dates.Take(1).Concat(dates.TakeLast(119)).ToList();
        }

        return dates
            .Select(date =>
            {
                var positions = positionCalculator.Calculate(
                    portfolios,
                    assets,
                    transactions.Where(transaction => transaction.Date <= date).ToList(),
                    prices.Where(price => price.Date <= date).ToList(),
                    repartitions);

                var positionsByPortfolio = positions
                    .GroupBy(position => position.PortfolioId)
                    .ToDictionary(group => group.Key);

                var portfolioPoints = portfolios
                    .OrderBy(portfolio => portfolio.Name)
                    .Select(portfolio =>
                    {
                        positionsByPortfolio.TryGetValue(portfolio.Id, out var portfolioPositions);
                        var marketValue = portfolioPositions?.Sum(position => position.MarketValue) ?? 0m;
                        var invested = portfolioPositions?.Sum(position => position.InvestedAmount) ?? 0m;
                        var gain = portfolioPositions?.Sum(position => position.UnrealizedGain) ?? 0m;

                        return new PortfolioHistoryPointDto(
                            portfolio.Id,
                            portfolio.Name,
                            Round(marketValue),
                            Round(invested),
                            Round(gain));
                    })
                    .ToList();

                var totalMarketValue = portfolioPoints.Sum(point => point.MarketValue);
                var totalInvested = portfolioPoints.Sum(point => point.InvestedAmount);

                return new DashboardHistoryPointDto(
                    date,
                    Round(totalMarketValue),
                    Round(totalInvested),
                    Round(totalMarketValue - totalInvested),
                    portfolioPoints);
            })
            .ToList();
    }

    private static decimal Round(decimal value, int decimals = 4) =>
        decimal.Round(value, decimals, MidpointRounding.AwayFromZero);
}
