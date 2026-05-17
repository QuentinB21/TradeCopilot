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
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(transaction => transaction.Quantity).HasPrecision(24, 8);
            entity.Property(transaction => transaction.UnitPrice).HasPrecision(18, 6);
            entity.Property(transaction => transaction.Fees).HasPrecision(18, 4);
            entity.Property(transaction => transaction.Currency).HasMaxLength(3);
            entity.Property(transaction => transaction.Comment).HasMaxLength(800);
        });

        modelBuilder.Entity<AssetPrice>(entity =>
        {
            entity.HasIndex(price => new { price.AssetId, price.Date }).IsUnique();
            entity.Property(price => price.Open).HasPrecision(18, 6);
            entity.Property(price => price.High).HasPrecision(18, 6);
            entity.Property(price => price.Low).HasPrecision(18, 6);
            entity.Property(price => price.Close).HasPrecision(18, 6);
            entity.Property(price => price.Currency).HasMaxLength(3);
            entity.Property(price => price.Source).HasMaxLength(80);
        });

        modelBuilder.Entity<AllocationRule>(entity =>
        {
            entity.HasIndex(rule => new { rule.PortfolioId, rule.AssetId }).IsUnique();
            entity.Property(rule => rule.TargetWeight).HasPrecision(9, 6);
            entity.Property(rule => rule.MinWeight).HasPrecision(9, 6);
            entity.Property(rule => rule.MaxWeight).HasPrecision(9, 6);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.Property(report => report.ContentJson).HasColumnType("jsonb");
        });
    }
}
