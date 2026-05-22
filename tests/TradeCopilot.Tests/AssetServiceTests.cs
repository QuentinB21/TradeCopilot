using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Assets;
using TradeCopilot.Application.Services.Assets;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class AssetServiceTests
{
    [Fact]
    public async Task Rejects_legacy_status_that_is_not_an_asset_strategic_role()
    {
        var repository = new FakeInvestmentRepository();
        var service = new AssetService(repository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAssetAsync(
            new CreateAssetRequest(
                "Test asset",
                "TEST",
                null,
                AssetType.Stock,
                "EUR",
                null,
                null,
                null,
                null,
                StrategicStatus.Frozen)));

        Assert.Contains("role strategique", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repository.AddedAssets);
    }

    private sealed class FakeInvestmentRepository : IInvestmentRepository
    {
        public List<Asset> AddedAssets { get; } = [];

        public Task AddAssetAsync(Asset asset, CancellationToken cancellationToken = default)
        {
            AddedAssets.Add(asset);
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
        public Task<IReadOnlyList<Repartition>> GetAssetRepartitionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Repartition?> GetAssetRepartitionByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StrategyRule>> GetStrategyRulesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StrategyRule?> GetStrategyRuleByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddTransactionsAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddPriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdatePriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
