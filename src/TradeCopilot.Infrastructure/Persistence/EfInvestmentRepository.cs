using Microsoft.EntityFrameworkCore;
using TradeCopilot.Application.Abstractions;
using TradeCopilot.Domain;

namespace TradeCopilot.Infrastructure.Persistence;

public sealed class EfInvestmentRepository(TradeCopilotDbContext dbContext) : IInvestmentRepository
{
    public async Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Portfolios.AsNoTracking().Include(portfolio => portfolio.Repartitions).ToListAsync(cancellationToken);

    public async Task<Portfolio?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Portfolios
            .Include(portfolio => portfolio.Repartitions)
            .FirstOrDefaultAsync(portfolio => portfolio.Id == id, cancellationToken);

    public async Task<bool> PortfolioHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AnyAsync(transaction => transaction.PortfolioId == id, cancellationToken)
        || await dbContext.Repartitions.AnyAsync(repartition => repartition.PortfolioId == id && repartition.Kind == RepartitionKind.PortfolioAsset, cancellationToken)
        || await dbContext.StrategyRules.AnyAsync(rule => rule.PortfolioId == id, cancellationToken);

    public async Task<IReadOnlyList<Asset>> GetAssetsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Assets.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Assets.FirstOrDefaultAsync(asset => asset.Id == id, cancellationToken);

    public async Task<bool> AssetHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AnyAsync(transaction => transaction.AssetId == id, cancellationToken)
        || await dbContext.AssetPrices.AnyAsync(price => price.AssetId == id, cancellationToken)
        || await dbContext.Repartitions.AnyAsync(repartition => repartition.AssetId == id, cancellationToken)
        || await dbContext.StrategyRules.AnyAsync(rule => rule.AssetId == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);

    public async Task<IReadOnlySet<string>> GetImportedTransactionExternalIdsAsync(string importSource, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.ImportSource == importSource && transaction.ExternalId != null)
            .Select(transaction => transaction.ExternalId!)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

    public async Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AssetPrices.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<AssetPrice?> GetPriceByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.AssetPrices.FirstOrDefaultAsync(price => price.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Repartition>> GetAssetRepartitionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Repartitions
            .AsNoTracking()
            .Where(repartition => repartition.Kind == RepartitionKind.PortfolioAsset)
            .ToListAsync(cancellationToken);

    public async Task<Repartition?> GetAssetRepartitionByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Repartitions.FirstOrDefaultAsync(
            repartition => repartition.Id == id && repartition.Kind == RepartitionKind.PortfolioAsset,
            cancellationToken);

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

    public async Task AddTransactionsAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        dbContext.Transactions.Update(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPriceAsync(AssetPrice price, CancellationToken cancellationToken = default)
    {
        dbContext.AssetPrices.Add(price);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePriceAsync(AssetPrice price, CancellationToken cancellationToken = default)
    {
        dbContext.AssetPrices.Update(price);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePriceAsync(AssetPrice price, CancellationToken cancellationToken = default)
    {
        dbContext.AssetPrices.Remove(price);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default)
    {
        dbContext.Repartitions.Add(repartition);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default)
    {
        dbContext.Repartitions.Update(repartition);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default)
    {
        dbContext.Repartitions.Remove(repartition);
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
