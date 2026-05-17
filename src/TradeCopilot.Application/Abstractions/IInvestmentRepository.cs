using TradeCopilot.Domain;

namespace TradeCopilot.Application.Abstractions;

public interface IInvestmentRepository
{
    Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken cancellationToken = default);
    Task<Portfolio?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Asset>> GetAssetsAsync(CancellationToken cancellationToken = default);
    Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllocationRule>> GetAllocationRulesAsync(CancellationToken cancellationToken = default);
    Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task DeletePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task AddAssetAsync(Asset asset, CancellationToken cancellationToken = default);
    Task UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default);
    Task DeleteAssetAsync(Asset asset, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task AddPriceAsync(AssetPrice price, CancellationToken cancellationToken = default);
}
