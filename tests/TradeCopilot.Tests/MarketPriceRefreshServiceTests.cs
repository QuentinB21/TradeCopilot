using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.MarketData;
using TradeCopilot.Application.Services.Prices;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class MarketPriceRefreshServiceTests
{
    [Fact]
    public async Task Refreshes_missing_price_for_held_asset()
    {
        var assetId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = assetId,
            Name = "Microsoft",
            Symbol = "MSFT",
            Type = AssetType.Stock,
            Currency = "EUR",
            StrategicStatus = StrategicStatus.Core
        };
        var transaction = Buy(assetId);
        var repository = new FakeInvestmentRepository();
        var provider = new FakeMarketDataProvider(new MarketQuoteDto(
            "MSFT",
            new DateOnly(2026, 5, 21),
            420m,
            430m,
            410m,
            425m,
            "EUR",
            "test-provider",
            DateTimeOffset.UtcNow));

        var prices = await new MarketPriceRefreshService(repository, provider)
            .RefreshCurrentPricesAsync([asset], [transaction], []);

        var price = Assert.Single(prices);
        Assert.Equal(assetId, price.AssetId);
        Assert.Equal(425m, price.Close);
        Assert.Equal("test-provider:normalized-v2", price.Source);
        Assert.Equal(1, provider.Calls);
        Assert.Single(repository.AddedPrices);
    }

    [Fact]
    public async Task Does_not_refresh_recent_price()
    {
        var assetId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = assetId,
            Name = "Microsoft",
            Symbol = "MSFT",
            Type = AssetType.Stock,
            Currency = "EUR",
            StrategicStatus = StrategicStatus.Core
        };
        var existingPrice = new AssetPrice
        {
            AssetId = assetId,
            Date = new DateOnly(2026, 5, 21),
            Close = 425m,
            Currency = "EUR",
            Source = "existing:normalized-v2",
            RetrievedAt = DateTimeOffset.UtcNow
        };
        var provider = new FakeMarketDataProvider(null);

        var prices = await new MarketPriceRefreshService(new FakeInvestmentRepository(), provider)
            .RefreshCurrentPricesAsync([asset], [Buy(assetId)], [existingPrice]);

        Assert.Same(existingPrice, Assert.Single(prices));
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Resolves_quote_from_isin_when_symbol_has_no_direct_quote()
    {
        var assetId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = assetId,
            Name = "iShares MSCI World Swap PEA",
            Symbol = "IE0002XZSHO1",
            Isin = "IE0002XZSHO1",
            Type = AssetType.Etf,
            Currency = "EUR",
            StrategicStatus = StrategicStatus.Core
        };
        var provider = new FakeMarketDataProvider(
            quote: null,
            searchResults:
            [
                new InstrumentSearchResultDto("WPEA.PA", "iShares MSCI World Swap PEA", "PAR", "Paris", "ETF", "EUR", null, AssetType.Etf, "test-provider")
            ],
            quotesBySymbol: new Dictionary<string, MarketQuoteDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["WPEA.PA"] = new(
                    "WPEA.PA",
                    new DateOnly(2026, 5, 21),
                    6.60m,
                    6.70m,
                    6.55m,
                    6.65m,
                    "EUR",
                    "test-provider",
                    DateTimeOffset.UtcNow)
            });

        var prices = await new MarketPriceRefreshService(new FakeInvestmentRepository(), provider)
            .RefreshCurrentPricesAsync([asset], [Buy(assetId)], []);

        Assert.Equal(6.65m, Assert.Single(prices).Close);
        Assert.Equal(2, provider.Calls);
        Assert.Equal(1, provider.SearchCalls);
    }

    private static Transaction Buy(Guid assetId) => new()
    {
        PortfolioId = Guid.NewGuid(),
        AssetId = assetId,
        Type = TransactionType.Buy,
        Date = new DateOnly(2026, 5, 21),
        Quantity = 1m,
        UnitPrice = 100m,
        Fees = 0m,
        Currency = "EUR"
    };

    private sealed class FakeMarketDataProvider(
        MarketQuoteDto? quote,
        IReadOnlyList<InstrumentSearchResultDto>? searchResults = null,
        IReadOnlyDictionary<string, MarketQuoteDto>? quotesBySymbol = null) : IMarketDataProvider
    {
        public int Calls { get; private set; }
        public int SearchCalls { get; private set; }

        public Task<IReadOnlyList<InstrumentSearchResultDto>> SearchInstrumentsAsync(string query, CancellationToken cancellationToken = default)
        {
            SearchCalls++;
            return Task.FromResult(searchResults ?? []);
        }

        public Task<MarketQuoteDto?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (quotesBySymbol is not null && quotesBySymbol.TryGetValue(symbol, out var symbolQuote))
            {
                return Task.FromResult<MarketQuoteDto?>(symbolQuote);
            }

            return Task.FromResult(quote);
        }
    }

    private sealed class FakeInvestmentRepository : IInvestmentRepository
    {
        public List<AssetPrice> AddedPrices { get; } = [];
        public List<AssetPrice> UpdatedPrices { get; } = [];

        public Task AddPriceAsync(AssetPrice price, CancellationToken cancellationToken = default)
        {
            AddedPrices.Add(price);
            return Task.CompletedTask;
        }

        public Task UpdatePriceAsync(AssetPrice price, CancellationToken cancellationToken = default)
        {
            UpdatedPrices.Add(price);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Portfolio?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> PortfolioHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Asset>> GetAssetsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AssetHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlySet<string>> GetImportedTransactionExternalIdsAsync(string importSource, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AssetPrice?> GetPriceByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AllocationRule>> GetAllocationRulesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AllocationRule?> GetAllocationRuleByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StrategyRule>> GetStrategyRulesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StrategyRule?> GetStrategyRuleByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddTransactionsAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
