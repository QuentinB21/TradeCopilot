using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Imports;
using TradeCopilot.Application.Services.Imports;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class TransactionImportServiceTests
{
    [Fact]
    public async Task Imports_only_unknown_external_transactions()
    {
        var portfolioId = Guid.NewGuid();
        var repository = new FakeInvestmentRepository(portfolioId, ["known-id"]);
        var strategy = new FakeImportStrategy([
            Candidate("known-id"),
            Candidate("new-id")
        ]);
        var service = new TransactionImportService(repository, [strategy]);

        var result = await service.ImportAsync(
            new TransactionImportRequest(TransactionImportProvider.TradeRepublic, portfolioId, "transactions.csv"),
            new MemoryStream());

        Assert.NotNull(result);
        Assert.Equal(1, result.ImportedTransactions);
        Assert.Equal(1, result.DuplicateRows);
        Assert.Single(repository.AddedTransactions);
        Assert.Equal("new-id", repository.AddedTransactions[0].ExternalId);
    }

    private static ImportedTransactionCandidate Candidate(string externalId) => new(
        2,
        externalId,
        TransactionType.Deposit,
        new DateOnly(2026, 1, 1),
        1m,
        100m,
        0m,
        "EUR",
        "test",
        null);

    private sealed class FakeImportStrategy(IReadOnlyList<ImportedTransactionCandidate> candidates) : ITransactionImportStrategy
    {
        public TransactionImportProvider Provider => TransactionImportProvider.TradeRepublic;

        public Task<TransactionImportParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransactionImportParseResult(candidates.Count, candidates, []));
    }

    private sealed class FakeInvestmentRepository(Guid portfolioId, IReadOnlyCollection<string> knownExternalIds) : IInvestmentRepository
    {
        public List<Transaction> AddedTransactions { get; } = [];

        public Task<Portfolio?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Portfolio?>(id == portfolioId
                ? new Portfolio { Id = portfolioId, Name = "Trade Republic", Broker = "Trade Republic", BaseCurrency = "EUR", Type = PortfolioType.SecuritiesAccount }
                : null);

        public Task<IReadOnlySet<string>> GetImportedTransactionExternalIdsAsync(string importSource, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(knownExternalIds.ToHashSet(StringComparer.OrdinalIgnoreCase));

        public Task<IReadOnlyList<Asset>> GetAssetsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Asset>>([]);

        public Task AddTransactionsAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default)
        {
            AddedTransactions.AddRange(transactions);
            return Task.CompletedTask;
        }

        public Task AddAssetAsync(Asset asset, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> PortfolioHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AssetHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AssetPrice?> GetPriceByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AllocationRule>> GetAllocationRulesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AllocationRule?> GetAllocationRuleByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StrategyRule>> GetStrategyRulesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StrategyRule?> GetStrategyRuleByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddPriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdatePriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
