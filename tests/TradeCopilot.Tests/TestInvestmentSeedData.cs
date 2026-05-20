using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public static class TestInvestmentSeedData
{
    public static TestInvestmentSeed Create()
    {
        var pea = new Portfolio
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "PEA BoursoBank",
            Type = PortfolioType.Pea,
            Broker = "BoursoBank",
            BaseCurrency = "EUR",
            CashBalance = 0m,
            TargetWeight = 0.80m
        };

        var tradeRepublic = new Portfolio
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = "Trade Republic",
            Type = PortfolioType.SecuritiesAccount,
            Broker = "Trade Republic",
            BaseCurrency = "EUR",
            CashBalance = 0m,
            TargetWeight = 0.20m
        };

        var wpea = Asset(Guid.Parse("20000000-0000-0000-0000-000000000001"), "iShares MSCI World Swap PEA", "WPEA", "IE0002XZSHO1", AssetType.Etf, "Global", StrategicStatus.Core);
        var techEtf = Asset(Guid.Parse("20000000-0000-0000-0000-000000000002"), "ETF S&P 500 Information Technology", "SP500IT", null, AssetType.Etf, "Technology", StrategicStatus.Conviction);
        var nvidia = Asset(Guid.Parse("20000000-0000-0000-0000-000000000003"), "NVIDIA", "NVDA", null, AssetType.Stock, "Technology", StrategicStatus.Conviction);
        var microsoft = Asset(Guid.Parse("20000000-0000-0000-0000-000000000004"), "Microsoft", "MSFT", null, AssetType.Stock, "Technology", StrategicStatus.Conviction);
        var amazon = Asset(Guid.Parse("20000000-0000-0000-0000-000000000005"), "Amazon", "AMZN", null, AssetType.Stock, "Consumer Discretionary", StrategicStatus.Conviction);
        var apple = Asset(Guid.Parse("20000000-0000-0000-0000-000000000006"), "Apple", "AAPL", null, AssetType.Stock, "Technology", StrategicStatus.Conviction);
        var palantir = Asset(Guid.Parse("20000000-0000-0000-0000-000000000007"), "Palantir", "PLTR", null, AssetType.Stock, "Technology", StrategicStatus.Observation);
        var spotify = Asset(Guid.Parse("20000000-0000-0000-0000-000000000008"), "Spotify", "SPOT", null, AssetType.Stock, "Communication Services", StrategicStatus.PlannedExit);
        var microStrategy = Asset(Guid.Parse("20000000-0000-0000-0000-000000000009"), "Strategy / MicroStrategy", "MSTR", null, AssetType.Stock, "Technology", StrategicStatus.PlannedExit);
        var oracle = Asset(Guid.Parse("20000000-0000-0000-0000-000000000010"), "Oracle", "ORCL", null, AssetType.Stock, "Technology", StrategicStatus.Frozen);

        var assets = new[] { wpea, techEtf, nvidia, microsoft, amazon, apple, palantir, spotify, microStrategy, oracle };

        var transactions = new[]
        {
            Buy(pea.Id, wpea.Id, new DateOnly(2026, 1, 12), 14.20m, 5.00m, 1.00m),
            Buy(pea.Id, wpea.Id, new DateOnly(2026, 2, 12), 13.65m, 7.00m, 1.00m),
            Buy(tradeRepublic.Id, techEtf.Id, new DateOnly(2026, 1, 15), 220.00m, 0.60m, 1.00m),
            Buy(tradeRepublic.Id, nvidia.Id, new DateOnly(2026, 1, 15), 135.00m, 0.80m, 1.00m),
            Buy(tradeRepublic.Id, microsoft.Id, new DateOnly(2026, 1, 15), 420.00m, 0.20m, 1.00m),
            Buy(tradeRepublic.Id, amazon.Id, new DateOnly(2026, 2, 15), 185.00m, 0.25m, 1.00m),
            Buy(tradeRepublic.Id, apple.Id, new DateOnly(2026, 2, 15), 190.00m, 0.20m, 1.00m),
            Buy(tradeRepublic.Id, palantir.Id, new DateOnly(2026, 2, 15), 24.00m, 2.00m, 1.00m)
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prices = new[]
        {
            Price(wpea.Id, today, 5.35m),
            Price(techEtf.Id, today, 238.00m),
            Price(nvidia.Id, today, 150.00m),
            Price(microsoft.Id, today, 438.00m),
            Price(amazon.Id, today, 192.00m),
            Price(apple.Id, today, 184.00m),
            Price(palantir.Id, today, 31.00m),
            Price(spotify.Id, today, 290.00m),
            Price(microStrategy.Id, today, 360.00m),
            Price(oracle.Id, today, 112.00m)
        };

        var allocationRules = new[]
        {
            Rule(pea.Id, wpea.Id, 1.00m, AllocationRuleStatus.Active),
            Rule(tradeRepublic.Id, techEtf.Id, 0.40m, AllocationRuleStatus.Active),
            Rule(tradeRepublic.Id, nvidia.Id, 0.25m, AllocationRuleStatus.Active),
            Rule(tradeRepublic.Id, microsoft.Id, 0.15m, AllocationRuleStatus.Active),
            Rule(tradeRepublic.Id, amazon.Id, 0.10m, AllocationRuleStatus.Active),
            Rule(tradeRepublic.Id, apple.Id, 0.10m, AllocationRuleStatus.Active),
            Rule(tradeRepublic.Id, palantir.Id, 0.00m, AllocationRuleStatus.Frozen),
            Rule(tradeRepublic.Id, spotify.Id, 0.00m, AllocationRuleStatus.ExitOnly),
            Rule(tradeRepublic.Id, microStrategy.Id, 0.00m, AllocationRuleStatus.ExitOnly),
            Rule(tradeRepublic.Id, oracle.Id, 0.00m, AllocationRuleStatus.Frozen)
        };

        var strategyRules = new[]
        {
            StrategyRule(pea.Id, wpea.Id, "Achat mensuel WPEA", "Acheter regulierement le socle long terme.", "Entre le 10 et le 15 du mois", "Renforcer WPEA selon la cle du portefeuille.", 10),
            StrategyRule(pea.Id, wpea.Id, "Correction WPEA", "Renforcer en cas de correction importante si du cash est disponible.", "Baisse superieure a 10%", "Prioriser WPEA apres validation humaine.", 20),
            StrategyRule(tradeRepublic.Id, palantir.Id, "Palantir en observation", "Conservation possible, mais pas de renfort tant que l'activite n'est pas suffisamment comprise.", null, "Ne pas renforcer.", 30),
            StrategyRule(tradeRepublic.Id, spotify.Id, "Spotify sortie planifiee", "Ligne non strategique.", "Rebond significatif", "Preparer une sortie apres validation humaine.", 40)
        };

        return new TestInvestmentSeed([pea, tradeRepublic], assets, transactions, prices, allocationRules, strategyRules);
    }

    private static Asset Asset(Guid id, string name, string symbol, string? isin, AssetType type, string sector, StrategicStatus status) => new()
    {
        Id = id,
        Name = name,
        Symbol = symbol,
        Isin = isin,
        Type = type,
        Currency = "EUR",
        Sector = sector,
        Country = "US",
        PriceProvider = "manual-seed",
        StrategicStatus = status
    };

    private static Transaction Buy(Guid portfolioId, Guid assetId, DateOnly date, decimal unitPrice, decimal quantity, decimal fees) => new()
    {
        PortfolioId = portfolioId,
        AssetId = assetId,
        Type = TransactionType.Buy,
        Date = date,
        UnitPrice = unitPrice,
        Quantity = quantity,
        Fees = fees,
        Currency = "EUR"
    };

    private static AssetPrice Price(Guid assetId, DateOnly date, decimal close) => new()
    {
        AssetId = assetId,
        Date = date,
        Close = close,
        Currency = "EUR",
        Source = "manual-seed"
    };

    private static AllocationRule Rule(Guid portfolioId, Guid assetId, decimal targetWeight, AllocationRuleStatus status) => new()
    {
        PortfolioId = portfolioId,
        AssetId = assetId,
        TargetWeight = targetWeight,
        Status = status
    };

    private static StrategyRule StrategyRule(Guid? portfolioId, Guid? assetId, string name, string description, string? triggerCondition, string recommendedAction, int priority) => new()
    {
        PortfolioId = portfolioId,
        AssetId = assetId,
        Name = name,
        Description = description,
        TriggerCondition = triggerCondition,
        RecommendedAction = recommendedAction,
        Priority = priority,
        IsActive = true
    };
}

public sealed record TestInvestmentSeed(
    IReadOnlyList<Portfolio> Portfolios,
    IReadOnlyList<Asset> Assets,
    IReadOnlyList<Transaction> Transactions,
    IReadOnlyList<AssetPrice> Prices,
    IReadOnlyList<AllocationRule> AllocationRules,
    IReadOnlyList<StrategyRule> StrategyRules);
