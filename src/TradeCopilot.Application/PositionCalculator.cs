using TradeCopilot.Application.Contracts.Positions;
using TradeCopilot.Domain;

namespace TradeCopilot.Application;

public sealed class PositionCalculator
{
    public IReadOnlyList<PositionDto> Calculate(
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<AssetPrice> prices,
        IReadOnlyList<AllocationRule> allocationRules)
    {
        var assetsById = assets.ToDictionary(asset => asset.Id);
        var portfoliosById = portfolios.ToDictionary(portfolio => portfolio.Id);
        var latestPriceByAssetId = prices
            .GroupBy(price => price.AssetId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(price => price.Date).First());

        var rawPositions = new List<PositionAccumulator>();

        foreach (var group in transactions
            .Where(transaction => transaction.AssetId.HasValue)
            .GroupBy(transaction => new { transaction.PortfolioId, AssetId = transaction.AssetId!.Value }))
        {
            if (!assetsById.TryGetValue(group.Key.AssetId, out var asset) ||
                !portfoliosById.TryGetValue(group.Key.PortfolioId, out var portfolio))
            {
                continue;
            }

            var quantity = 0m;
            var costBasis = 0m;
            var realizedGain = 0m;

            foreach (var transaction in group.OrderBy(transaction => transaction.Date))
            {
                switch (transaction.Type)
                {
                    case TransactionType.Buy:
                        quantity += transaction.Quantity;
                        costBasis += transaction.Quantity * transaction.UnitPrice + transaction.Fees;
                        break;

                    case TransactionType.Sell:
                        if (quantity <= 0m)
                        {
                            break;
                        }

                        var soldQuantity = Math.Min(transaction.Quantity, quantity);
                        var averageCost = costBasis / quantity;
                        var removedCostBasis = soldQuantity * averageCost;
                        var proceeds = soldQuantity * transaction.UnitPrice - transaction.Fees;

                        realizedGain += proceeds - removedCostBasis;
                        quantity -= soldQuantity;
                        costBasis -= removedCostBasis;
                        break;

                    case TransactionType.Split:
                        if (transaction.Quantity > 0m)
                        {
                            quantity *= transaction.Quantity;
                        }
                        break;
                }
            }

            if (quantity <= 0m)
            {
                continue;
            }

            var averageBuyPrice = costBasis / quantity;
            var hasMarketPrice = latestPriceByAssetId.TryGetValue(asset.Id, out var latestPrice);
            var marketPrice = hasMarketPrice ? latestPrice!.Close : averageBuyPrice;
            rawPositions.Add(new PositionAccumulator(
                portfolio,
                asset,
                quantity,
                costBasis,
                marketPrice,
                quantity * marketPrice,
                averageBuyPrice,
                hasMarketPrice,
                realizedGain));
        }

        var totalByPortfolio = rawPositions
            .GroupBy(position => position.Portfolio.Id)
            .ToDictionary(group => group.Key, group => group.Sum(position => position.MarketValue));

        var targetWeightByPortfolioAsset = allocationRules.ToDictionary(
            rule => (rule.PortfolioId, rule.AssetId),
            rule => rule.TargetWeight);

        return rawPositions.Select(position =>
        {
            var portfolioTotal = totalByPortfolio[position.Portfolio.Id];
            var weight = portfolioTotal > 0m ? position.MarketValue / portfolioTotal : 0m;
            targetWeightByPortfolioAsset.TryGetValue((position.Portfolio.Id, position.Asset.Id), out var targetWeight);
            var unrealizedGain = position.MarketValue - position.CostBasis;

            return new PositionDto(
                position.Portfolio.Id,
                position.Portfolio.Name,
                position.Asset.Id,
                position.Asset.Name,
                position.Asset.Symbol,
                position.Asset.StrategicStatus,
                DecimalRound(position.Quantity, 6),
                DecimalRound(position.AverageBuyPrice),
                DecimalRound(position.CostBasis),
                DecimalRound(position.MarketPrice),
                position.HasMarketPrice,
                DecimalRound(position.MarketValue),
                DecimalRound(unrealizedGain),
                position.CostBasis > 0m ? DecimalRound(unrealizedGain / position.CostBasis) : 0m,
                DecimalRound(position.RealizedGain),
                DecimalRound(weight),
                targetWeight > 0m ? DecimalRound(targetWeight) : null,
                targetWeight > 0m ? DecimalRound(weight - targetWeight) : null);
        }).ToList();
    }

    private static decimal DecimalRound(decimal value, int decimals = 4) =>
        decimal.Round(value, decimals, MidpointRounding.AwayFromZero);

    private sealed record PositionAccumulator(
        Portfolio Portfolio,
        Asset Asset,
        decimal Quantity,
        decimal CostBasis,
        decimal MarketPrice,
        decimal MarketValue,
        decimal AverageBuyPrice,
        bool HasMarketPrice,
        decimal RealizedGain);
}
