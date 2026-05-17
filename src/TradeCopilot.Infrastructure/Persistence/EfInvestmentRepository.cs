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

    public async Task<bool> PortfolioHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AnyAsync(transaction => transaction.PortfolioId == id, cancellationToken)
        || await dbContext.AllocationRules.AnyAsync(rule => rule.PortfolioId == id, cancellationToken)
        || await dbContext.StrategyRules.AnyAsync(rule => rule.PortfolioId == id, cancellationToken);

    public async Task<IReadOnlyList<Asset>> GetAssetsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Assets.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Assets.FirstOrDefaultAsync(asset => asset.Id == id, cancellationToken);

    public async Task<bool> AssetHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AnyAsync(transaction => transaction.AssetId == id, cancellationToken)
        || await dbContext.AssetPrices.AnyAsync(price => price.AssetId == id, cancellationToken)
        || await dbContext.AllocationRules.AnyAsync(rule => rule.AssetId == id, cancellationToken)
        || await dbContext.StrategyRules.AnyAsync(rule => rule.AssetId == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AssetPrices.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AllocationRule>> GetAllocationRulesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AllocationRules.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<AllocationRule?> GetAllocationRuleByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.AllocationRules.FirstOrDefaultAsync(rule => rule.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StrategyRule>> GetStrategyRulesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.StrategyRules.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<StrategyRule?> GetStrategyRuleByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.StrategyRules.FirstOrDefaultAsync(rule => rule.Id == id, cancellationToken);

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

    public async Task AddAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default)
    {
        dbContext.AllocationRules.Add(allocationRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default)
    {
        dbContext.AllocationRules.Update(allocationRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllocationRuleAsync(AllocationRule allocationRule, CancellationToken cancellationToken = default)
    {
        dbContext.AllocationRules.Remove(allocationRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default)
    {
        dbContext.StrategyRules.Add(strategyRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default)
    {
        dbContext.StrategyRules.Update(strategyRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default)
    {
        dbContext.StrategyRules.Remove(strategyRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
