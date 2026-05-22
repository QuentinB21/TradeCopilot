using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Portfolios;
using TradeCopilot.Application.Services.Portfolios;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class PortfolioServiceTests
{
    [Fact]
    public async Task Rejects_portfolio_when_global_target_sum_exceeds_one_hundred_percent()
    {
        var repository = new FakeInvestmentRepository([
            new Portfolio
            {
                Id = Guid.NewGuid(),
                Name = "PEA",
                Broker = "Broker",
                BaseCurrency = "EUR",
                Type = PortfolioType.Pea,
                Repartitions = [PortfolioRepartition(0.8m)]
            }
        ]);
        var service = new PortfolioService(repository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePortfolioAsync(
            new CreatePortfolioRequest(
                "Compte titres",
                PortfolioType.SecuritiesAccount,
                "Broker",
                "EUR",
                0m,
                0.3m)));

        Assert.Contains("100 %", exception.Message, StringComparison.Ordinal);
        Assert.Empty(repository.AddedPortfolios);
    }

    private sealed class FakeInvestmentRepository(IReadOnlyList<Portfolio> portfolios) : IInvestmentRepository
    {
        public List<Portfolio> AddedPortfolios { get; } = [];

        public Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(portfolios);

        public Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
        {
            AddedPortfolios.Add(portfolio);
            return Task.CompletedTask;
        }

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
        public Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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

    private static Repartition PortfolioRepartition(decimal targetWeight) => new()
    {
        Kind = RepartitionKind.Portfolio,
        TargetWeight = targetWeight
    };
}
