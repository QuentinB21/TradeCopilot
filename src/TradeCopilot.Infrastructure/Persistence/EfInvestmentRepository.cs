using Microsoft.EntityFrameworkCore;
using TradeCopilot.Application.Abstractions;
using TradeCopilot.Domain;

namespace TradeCopilot.Infrastructure.Persistence;

public sealed class EfInvestmentRepository(TradeCopilotDbContext dbContext, ICurrentUserContext currentUser) : IInvestmentRepository
{
    private string OwnerUserId => currentUser.UserId;

    public async Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Portfolios
            .AsNoTracking()
            .Where(portfolio => portfolio.OwnerUserId == OwnerUserId)
            .Include(portfolio => portfolio.Repartitions.Where(repartition => repartition.OwnerUserId == OwnerUserId))
            .ToListAsync(cancellationToken);

    public async Task<Portfolio?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Portfolios
            .Include(portfolio => portfolio.Repartitions)
            .FirstOrDefaultAsync(portfolio => portfolio.Id == id && portfolio.OwnerUserId == OwnerUserId, cancellationToken);

    public async Task<bool> PortfolioHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AnyAsync(transaction => transaction.OwnerUserId == OwnerUserId && transaction.PortfolioId == id, cancellationToken)
        || await dbContext.Repartitions.AnyAsync(repartition => repartition.OwnerUserId == OwnerUserId && repartition.PortfolioId == id && repartition.Kind == RepartitionKind.PortfolioAsset, cancellationToken)
        || await dbContext.StrategyRules.AnyAsync(rule => rule.OwnerUserId == OwnerUserId && rule.PortfolioId == id, cancellationToken);

    public async Task<IReadOnlyList<Asset>> GetAssetsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Assets
            .AsNoTracking()
            .Where(asset => asset.OwnerUserId == OwnerUserId)
            .ToListAsync(cancellationToken);

    public async Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Assets.FirstOrDefaultAsync(asset => asset.Id == id && asset.OwnerUserId == OwnerUserId, cancellationToken);

    public async Task<bool> AssetHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AnyAsync(transaction => transaction.OwnerUserId == OwnerUserId && transaction.AssetId == id, cancellationToken)
        || await dbContext.AssetPrices.AnyAsync(price => price.OwnerUserId == OwnerUserId && price.AssetId == id, cancellationToken)
        || await dbContext.Repartitions.AnyAsync(repartition => repartition.OwnerUserId == OwnerUserId && repartition.AssetId == id, cancellationToken)
        || await dbContext.StrategyRules.AnyAsync(rule => rule.OwnerUserId == OwnerUserId && rule.AssetId == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.OwnerUserId == OwnerUserId)
            .ToListAsync(cancellationToken);

    public async Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.FirstOrDefaultAsync(transaction => transaction.Id == id && transaction.OwnerUserId == OwnerUserId, cancellationToken);

    public async Task<IReadOnlySet<string>> GetImportedTransactionExternalIdsAsync(string importSource, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.OwnerUserId == OwnerUserId && transaction.ImportSource == importSource && transaction.ExternalId != null)
            .Select(transaction => transaction.ExternalId!)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

    public async Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AssetPrices
            .AsNoTracking()
            .Where(price => price.OwnerUserId == OwnerUserId)
            .ToListAsync(cancellationToken);

    public async Task<AssetPrice?> GetPriceByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.AssetPrices.FirstOrDefaultAsync(price => price.Id == id && price.OwnerUserId == OwnerUserId, cancellationToken);

    public async Task<IReadOnlyList<Repartition>> GetAssetRepartitionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Repartitions
            .AsNoTracking()
            .Where(repartition => repartition.OwnerUserId == OwnerUserId && repartition.Kind == RepartitionKind.PortfolioAsset)
            .ToListAsync(cancellationToken);

    public async Task<Repartition?> GetAssetRepartitionByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Repartitions.FirstOrDefaultAsync(
            repartition => repartition.Id == id && repartition.OwnerUserId == OwnerUserId && repartition.Kind == RepartitionKind.PortfolioAsset,
            cancellationToken);

    public async Task<IReadOnlyList<StrategyRule>> GetStrategyRulesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.StrategyRules
            .AsNoTracking()
            .Where(rule => rule.OwnerUserId == OwnerUserId)
            .ToListAsync(cancellationToken);

    public async Task<StrategyRule?> GetStrategyRuleByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.StrategyRules.FirstOrDefaultAsync(rule => rule.Id == id && rule.OwnerUserId == OwnerUserId, cancellationToken);

    public async Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        portfolio.OwnerUserId = OwnerUserId;
        foreach (var repartition in portfolio.Repartitions)
        {
            repartition.OwnerUserId = OwnerUserId;
        }

        dbContext.Portfolios.Add(portfolio);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        portfolio.OwnerUserId = OwnerUserId;
        foreach (var repartition in portfolio.Repartitions)
        {
            repartition.OwnerUserId = OwnerUserId;
        }

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
        asset.OwnerUserId = OwnerUserId;
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        asset.OwnerUserId = OwnerUserId;
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
        await EnsureOwnedPortfolioAsync(transaction.PortfolioId, cancellationToken);
        await EnsureOwnedAssetAsync(transaction.AssetId, cancellationToken);
        transaction.OwnerUserId = OwnerUserId;
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTransactionsAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        foreach (var transaction in transactions)
        {
            await EnsureOwnedPortfolioAsync(transaction.PortfolioId, cancellationToken);
            await EnsureOwnedAssetAsync(transaction.AssetId, cancellationToken);
            transaction.OwnerUserId = OwnerUserId;
        }

        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await EnsureOwnedPortfolioAsync(transaction.PortfolioId, cancellationToken);
        await EnsureOwnedAssetAsync(transaction.AssetId, cancellationToken);
        transaction.OwnerUserId = OwnerUserId;
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
        await EnsureOwnedAssetAsync(price.AssetId, cancellationToken);
        price.OwnerUserId = OwnerUserId;
        dbContext.AssetPrices.Add(price);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePriceAsync(AssetPrice price, CancellationToken cancellationToken = default)
    {
        await EnsureOwnedAssetAsync(price.AssetId, cancellationToken);
        price.OwnerUserId = OwnerUserId;
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
        await EnsureOwnedPortfolioAsync(repartition.PortfolioId, cancellationToken);
        await EnsureOwnedAssetAsync(repartition.AssetId, cancellationToken);
        repartition.OwnerUserId = OwnerUserId;
        dbContext.Repartitions.Add(repartition);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default)
    {
        await EnsureOwnedPortfolioAsync(repartition.PortfolioId, cancellationToken);
        await EnsureOwnedAssetAsync(repartition.AssetId, cancellationToken);
        repartition.OwnerUserId = OwnerUserId;
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
        await EnsureOwnedPortfolioAsync(strategyRule.PortfolioId, cancellationToken);
        await EnsureOwnedAssetAsync(strategyRule.AssetId, cancellationToken);
        strategyRule.OwnerUserId = OwnerUserId;
        dbContext.StrategyRules.Add(strategyRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default)
    {
        await EnsureOwnedPortfolioAsync(strategyRule.PortfolioId, cancellationToken);
        await EnsureOwnedAssetAsync(strategyRule.AssetId, cancellationToken);
        strategyRule.OwnerUserId = OwnerUserId;
        dbContext.StrategyRules.Update(strategyRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default)
    {
        dbContext.StrategyRules.Remove(strategyRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureOwnedPortfolioAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Portfolios.AnyAsync(
            portfolio => portfolio.Id == portfolioId && portfolio.OwnerUserId == OwnerUserId,
            cancellationToken);

        if (!exists)
        {
            throw new ArgumentException("Le portefeuille reference est introuvable pour l'utilisateur courant.", nameof(portfolioId));
        }
    }

    private async Task EnsureOwnedPortfolioAsync(Guid? portfolioId, CancellationToken cancellationToken)
    {
        if (portfolioId is not null)
        {
            await EnsureOwnedPortfolioAsync(portfolioId.Value, cancellationToken);
        }
    }

    private async Task EnsureOwnedAssetAsync(Guid? assetId, CancellationToken cancellationToken)
    {
        if (assetId is null)
        {
            return;
        }

        var exists = await dbContext.Assets.AnyAsync(
            asset => asset.Id == assetId.Value && asset.OwnerUserId == OwnerUserId,
            cancellationToken);

        if (!exists)
        {
            throw new ArgumentException("L'actif reference est introuvable pour l'utilisateur courant.", nameof(assetId));
        }
    }
}
