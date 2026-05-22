using TradeCopilot.Domain;

namespace TradeCopilot.Application.Abstractions;

public interface IInvestmentRepository
{
    Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken cancellationToken = default);
    Task<Portfolio?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PortfolioHasReferencesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Asset>> GetAssetsAsync(CancellationToken cancellationToken = default);
    Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AssetHasReferencesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken = default);
    Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetImportedTransactionExternalIdsAsync(string importSource, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default);
    Task<AssetPrice?> GetPriceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Repartition>> GetAssetRepartitionsAsync(CancellationToken cancellationToken = default);
    Task<Repartition?> GetAssetRepartitionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StrategyRule>> GetStrategyRulesAsync(CancellationToken cancellationToken = default);
    Task<StrategyRule?> GetStrategyRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task DeletePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task AddAssetAsync(Asset asset, CancellationToken cancellationToken = default);
    Task UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default);
    Task DeleteAssetAsync(Asset asset, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task AddTransactionsAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default);
    Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task AddPriceAsync(AssetPrice price, CancellationToken cancellationToken = default);
    Task UpdatePriceAsync(AssetPrice price, CancellationToken cancellationToken = default);
    Task DeletePriceAsync(AssetPrice price, CancellationToken cancellationToken = default);
    Task AddRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default);
    Task UpdateRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default);
    Task DeleteRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default);
    Task AddStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default);
    Task UpdateStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default);
    Task DeleteStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default);
}
