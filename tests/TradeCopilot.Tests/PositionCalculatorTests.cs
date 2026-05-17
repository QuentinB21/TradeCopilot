using TradeCopilot.Application;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class PositionCalculatorTests
{
    [Fact]
    public void Calculates_average_price_market_value_and_unrealized_gain()
    {
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            Name = "PEA BoursoBank",
            Type = PortfolioType.Pea,
            Broker = "BoursoBank",
            BaseCurrency = "EUR"
        };
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Name = "iShares MSCI World Swap PEA",
            Symbol = "WPEA",
            Type = AssetType.Etf,
            Currency = "EUR",
            StrategicStatus = StrategicStatus.Core
        };

        var transactions = new[]
        {
            Buy(portfolio.Id, asset.Id, new DateOnly(2026, 1, 10), 10m, 10m, 1m),
            Buy(portfolio.Id, asset.Id, new DateOnly(2026, 2, 10), 12m, 10m, 1m)
        };
        var prices = new[]
        {
            new AssetPrice { AssetId = asset.Id, Date = new DateOnly(2026, 2, 28), Close = 13m, Currency = "EUR", Source = "test" }
        };

        var positions = new PositionCalculator().Calculate([portfolio], [asset], transactions, prices, []);

        var position = Assert.Single(positions);
        Assert.Equal(20m, position.Quantity);
        Assert.Equal(11.1m, position.AverageBuyPrice);
        Assert.Equal(222m, position.InvestedAmount);
        Assert.Equal(260m, position.MarketValue);
        Assert.Equal(38m, position.UnrealizedGain);
    }

    [Fact]
    public void Calculates_realized_gain_after_partial_sale_using_average_cost()
    {
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            Name = "Trade Republic",
            Type = PortfolioType.SecuritiesAccount,
            Broker = "Trade Republic",
            BaseCurrency = "EUR"
        };
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Name = "NVIDIA",
            Symbol = "NVDA",
            Type = AssetType.Stock,
            Currency = "EUR",
            StrategicStatus = StrategicStatus.Conviction
        };

        var transactions = new[]
        {
            Buy(portfolio.Id, asset.Id, new DateOnly(2026, 1, 10), 100m, 2m, 0m),
            new Transaction
            {
                PortfolioId = portfolio.Id,
                AssetId = asset.Id,
                Type = TransactionType.Sell,
                Date = new DateOnly(2026, 2, 10),
                UnitPrice = 130m,
                Quantity = 1m,
                Fees = 1m,
                Currency = "EUR"
            }
        };
        var prices = new[]
        {
            new AssetPrice { AssetId = asset.Id, Date = new DateOnly(2026, 2, 28), Close = 140m, Currency = "EUR", Source = "test" }
        };

        var position = Assert.Single(new PositionCalculator().Calculate([portfolio], [asset], transactions, prices, []));

        Assert.Equal(1m, position.Quantity);
        Assert.Equal(100m, position.InvestedAmount);
        Assert.Equal(29m, position.RealizedGain);
        Assert.Equal(40m, position.UnrealizedGain);
    }

    private static Transaction Buy(Guid portfolioId, Guid assetId, DateOnly date, decimal unitPrice, decimal quantity, decimal fees) => new()
    {
        PortfolioId = portfolioId,
        AssetId = assetId,
        Type = TransactionType.Buy,
        Date = date,
        UnitPrice = unitPrice,
        Quantity = quantity,
        Fees = fees,
        Currency = "EUR"
    };
}
