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
    Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllocationRule>> GetAllocationRulesAsync(CancellationToken cancellationToken = default);
    Task<AllocationRule?> GetAllocationRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StrategyRule>> GetStrategyRulesAsync(CancellationToken cancellationToken = default);
    Task<StrategyRule?> GetStrategyRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task DeletePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task AddAssetAsync(Asset asset, CancellationToken cancellationToken = default);
    Task UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default);
    Task DeleteAssetAsync(Asset asset, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task AddPriceAsync(AssetPrice price, CancellationToken cancellationToken = default);
    Task AddAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default);
    Task UpdateAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default);
    Task DeleteAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default);
    Task AddStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default);
    Task UpdateStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default);
    Task DeleteStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default);
}
