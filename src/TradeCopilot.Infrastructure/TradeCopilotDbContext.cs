using Microsoft.EntityFrameworkCore;
using TradeCopilot.Domain;

namespace TradeCopilot.Infrastructure;

public sealed class TradeCopilotDbContext(DbContextOptions<TradeCopilotDbContext> options) : DbContext(options)
{
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AssetPrice> AssetPrices => Set<AssetPrice>();
    public DbSet<AllocationRule> AllocationRules => Set<AllocationRule>();
    public DbSet<StrategyRule> StrategyRules => Set<StrategyRule>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<InvestmentJournalEntry> InvestmentJournalEntries => Set<InvestmentJournalEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.Property(portfolio => portfolio.Name).HasMaxLength(160);
            entity.Property(portfolio => portfolio.Broker).HasMaxLength(120);
            entity.Property(portfolio => portfolio.BaseCurrency).HasMaxLength(3);
            entity.Property(portfolio => portfolio.CashBalance).HasPrecision(18, 4);
            entity.Property(portfolio => portfolio.TargetWeight).HasPrecision(9, 6);
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasIndex(asset => asset.Symbol).IsUnique();
            entity.Property(asset => asset.Name).HasMaxLength(200);
            entity.Property(asset => asset.Symbol).HasMaxLength(32);
            entity.Property(asset => asset.Isin).HasMaxLength(12);
            entity.Property(asset => asset.Currency).HasMaxLength(3);
            entity.Property(asset => asset.Sector).HasMaxLength(120);
            entity.Property(asset => asset.Country).HasMaxLength(120);
            entity.Property(asset => asset.PriceProvider).HasMaxLength(80);
            entity.Property(asset => asset.MarketSymbol).HasMaxLength(48);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasOne(transaction => transaction.Portfolio)
                .WithMany(portfolio => portfolio.Transactions)
                .HasForeignKey(transaction => transaction.PortfolioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(transaction => transaction.Asset)
                .WithMany()
                .HasForeignKey(transaction => transaction.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(transaction => transaction.Quantity).HasPrecision(24, 8);
            entity.Property(transaction => transaction.UnitPrice).HasPrecision(18, 6);
            entity.Property(transaction => transaction.Fees).HasPrecision(18, 4);
            entity.Property(transaction => transaction.Currency).HasMaxLength(3);
            entity.Property(transaction => transaction.Comment).HasMaxLength(800);
            entity.Property(transaction => transaction.ImportSource).HasMaxLength(80);
            entity.Property(transaction => transaction.ExternalId).HasMaxLength(160);
            entity.HasIndex(transaction => new { transaction.ImportSource, transaction.ExternalId })
                .IsUnique()
                .HasFilter("\"ImportSource\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");
        });

        modelBuilder.Entity<AssetPrice>(entity =>
        {
            entity.HasOne(price => price.Asset)
                .WithMany(asset => asset.Prices)
                .HasForeignKey(price => price.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(price => new { price.AssetId, price.Date }).IsUnique();
            entity.Property(price => price.Open).HasPrecision(18, 6);
            entity.Property(price => price.High).HasPrecision(18, 6);
            entity.Property(price => price.Low).HasPrecision(18, 6);
            entity.Property(price => price.Close).HasPrecision(18, 6);
            entity.Property(price => price.Currency).HasMaxLength(3);
            entity.Property(price => price.Source).HasMaxLength(80);
            entity.Property(price => price.RetrievedAt);
        });

        modelBuilder.Entity<AllocationRule>(entity =>
        {
            entity.HasOne(rule => rule.Portfolio)
                .WithMany(portfolio => portfolio.AllocationRules)
                .HasForeignKey(rule => rule.PortfolioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(rule => rule.Asset)
                .WithMany()
                .HasForeignKey(rule => rule.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(rule => new { rule.PortfolioId, rule.AssetId }).IsUnique();
            entity.Property(rule => rule.TargetWeight).HasPrecision(9, 6);
            entity.Property(rule => rule.MinWeight).HasPrecision(9, 6);
            entity.Property(rule => rule.MaxWeight).HasPrecision(9, 6);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.Property(report => report.ContentJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<StrategyRule>(entity =>
        {
            entity.HasOne(rule => rule.Portfolio)
                .WithMany()
                .HasForeignKey(rule => rule.PortfolioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(rule => rule.Asset)
                .WithMany()
                .HasForeignKey(rule => rule.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(rule => rule.Name).HasMaxLength(160);
            entity.Property(rule => rule.Description).HasMaxLength(1200);
            entity.Property(rule => rule.TriggerCondition).HasMaxLength(800);
            entity.Property(rule => rule.RecommendedAction).HasMaxLength(1200);
        });
    }
}
