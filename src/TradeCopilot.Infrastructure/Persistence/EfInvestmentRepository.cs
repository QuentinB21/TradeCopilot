using Microsoft.EntityFrameworkCore;
using TradeCopilot.Application.Abstractions;
using TradeCopilot.Domain;

namespace TradeCopilot.Infrastructure.Persistence;

public sealed class EfInvestmentRepository(TradeCopilotDbContext dbContext) : IInvestmentRepository
{
    public async Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Portfolios.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Portfolio?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Portfolios.FirstOrDefaultAsync(portfolio => portfolio.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Asset>> GetAssetsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Assets.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Assets.FirstOrDefaultAsync(asset => asset.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AssetPrices.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AllocationRule>> GetAllocationRulesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AllocationRules.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        dbContext.Portfolios.Add(portfolio);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        dbContext.Portfolios.Update(portfolio);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        dbContext.Portfolios.Remove(portfolio);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAssetAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        dbContext.Assets.Update(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAssetAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        dbContext.Assets.Remove(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPriceAsync(AssetPrice price, CancellationToken cancellationToken = default)
    {
        dbContext.AssetPrices.Add(price);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
