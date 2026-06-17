using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Domain;

namespace TradeCopilot.Infrastructure;

public sealed class DatabaseInitializer(IServiceProvider serviceProvider) : IHostedService
{
    private const string ExistingDataFallbackOwnerUserId = "local-development-user";
    private const string GuestOwnerUserId = "guest-demo";
    private static readonly JsonSerializerOptions RuleJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<TradeCopilotDbContext>();

        if (dbContext is null)
        {
            return;
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureJsonRuleDefinitionColumnAsync(dbContext, cancellationToken);
        await EnsureOwnerColumnsAsync(dbContext, cancellationToken);
        await EnsureGuestDemoDataAsync(dbContext, cancellationToken);

        // Real user workspaces stay empty; only the read-only guest workspace is seeded.
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureJsonRuleDefinitionColumnAsync(TradeCopilotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "StrategyRules"
            ADD COLUMN IF NOT EXISTS "DefinitionJson" jsonb NULL;
            """,
            cancellationToken);
    }

    private static async Task EnsureOwnerColumnsAsync(TradeCopilotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return;
        }

        foreach (var table in new[] { "Portfolios", "Assets", "Transactions", "AssetPrices", "Repartitions", "StrategyRules" })
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                OwnerColumnSql(table),
                cancellationToken);
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DROP INDEX IF EXISTS "IX_Assets_Symbol";
            DROP INDEX IF EXISTS "IX_Transactions_ImportSource_ExternalId";
            DROP INDEX IF EXISTS "IX_AssetPrices_AssetId_Date";
            DROP INDEX IF EXISTS "IX_Repartitions_PortfolioId";
            DROP INDEX IF EXISTS "IX_Repartitions_PortfolioId_AssetId";

            CREATE INDEX IF NOT EXISTS "IX_Portfolios_OwnerUserId"
                ON "Portfolios" ("OwnerUserId");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Assets_OwnerUserId_Symbol"
                ON "Assets" ("OwnerUserId", "Symbol");

            CREATE INDEX IF NOT EXISTS "IX_Transactions_OwnerUserId"
                ON "Transactions" ("OwnerUserId");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Transactions_OwnerUserId_ImportSource_ExternalId"
                ON "Transactions" ("OwnerUserId", "ImportSource", "ExternalId")
                WHERE "ImportSource" IS NOT NULL AND "ExternalId" IS NOT NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssetPrices_OwnerUserId_AssetId_Date"
                ON "AssetPrices" ("OwnerUserId", "AssetId", "Date");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Repartitions_OwnerUserId_PortfolioId"
                ON "Repartitions" ("OwnerUserId", "PortfolioId")
                WHERE "Kind" = 0;

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Repartitions_OwnerUserId_PortfolioId_AssetId"
                ON "Repartitions" ("OwnerUserId", "PortfolioId", "AssetId")
                WHERE "AssetId" IS NOT NULL;

            CREATE INDEX IF NOT EXISTS "IX_StrategyRules_OwnerUserId"
                ON "StrategyRules" ("OwnerUserId");
            """,
            cancellationToken);
    }

    private static string OwnerColumnSql(string table) => table switch
    {
        "Portfolios" => $"""ALTER TABLE "Portfolios" ADD COLUMN IF NOT EXISTS "OwnerUserId" character varying(128) NOT NULL DEFAULT '{ExistingDataFallbackOwnerUserId}';""",
        "Assets" => $"""ALTER TABLE "Assets" ADD COLUMN IF NOT EXISTS "OwnerUserId" character varying(128) NOT NULL DEFAULT '{ExistingDataFallbackOwnerUserId}';""",
        "Transactions" => $"""ALTER TABLE "Transactions" ADD COLUMN IF NOT EXISTS "OwnerUserId" character varying(128) NOT NULL DEFAULT '{ExistingDataFallbackOwnerUserId}';""",
        "AssetPrices" => $"""ALTER TABLE "AssetPrices" ADD COLUMN IF NOT EXISTS "OwnerUserId" character varying(128) NOT NULL DEFAULT '{ExistingDataFallbackOwnerUserId}';""",
        "Repartitions" => $"""ALTER TABLE "Repartitions" ADD COLUMN IF NOT EXISTS "OwnerUserId" character varying(128) NOT NULL DEFAULT '{ExistingDataFallbackOwnerUserId}';""",
        "StrategyRules" => $"""ALTER TABLE "StrategyRules" ADD COLUMN IF NOT EXISTS "OwnerUserId" character varying(128) NOT NULL DEFAULT '{ExistingDataFallbackOwnerUserId}';""",
        _ => throw new InvalidOperationException($"Unknown table {table}.")
    };

    private static async Task EnsureGuestDemoDataAsync(TradeCopilotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Portfolios.AnyAsync(portfolio => portfolio.OwnerUserId == GuestOwnerUserId, cancellationToken))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-170);

        var peaId = Guid.Parse("7a1b42d3-4b9f-4a43-9cf6-6eaa78d20101");
        var ctoId = Guid.Parse("7a1b42d3-4b9f-4a43-9cf6-6eaa78d20102");
        var worldId = Guid.Parse("7a1b42d3-4b9f-4a43-9cf6-6eaa78d20201");
        var sp500Id = Guid.Parse("7a1b42d3-4b9f-4a43-9cf6-6eaa78d20202");
        var europeId = Guid.Parse("7a1b42d3-4b9f-4a43-9cf6-6eaa78d20203");
        var energyId = Guid.Parse("7a1b42d3-4b9f-4a43-9cf6-6eaa78d20204");

        var portfolios = new[]
        {
            new Portfolio
            {
                Id = peaId,
                OwnerUserId = GuestOwnerUserId,
                Name = "PEA Demo",
                Type = PortfolioType.Pea,
                Broker = "Boursobank",
                BaseCurrency = "EUR",
                CashBalance = 230.45m,
                Repartitions =
                [
                    PortfolioRepartition(peaId, 0.75m)
                ]
            },
            new Portfolio
            {
                Id = ctoId,
                OwnerUserId = GuestOwnerUserId,
                Name = "CTO Demo",
                Type = PortfolioType.SecuritiesAccount,
                Broker = "Trade Republic",
                BaseCurrency = "EUR",
                CashBalance = 84.20m,
                Repartitions =
                [
                    PortfolioRepartition(ctoId, 0.25m)
                ]
            }
        };

        var assets = new[]
        {
            new Asset
            {
                Id = worldId,
                OwnerUserId = GuestOwnerUserId,
                Name = "ETF MSCI World",
                Symbol = "CW8",
                Isin = "LU1681043599",
                Type = AssetType.Etf,
                Currency = "EUR",
                Country = "Monde",
                PriceProvider = "demo",
                MarketSymbol = "CW8.PA",
                StrategicStatus = StrategicStatus.Core
            },
            new Asset
            {
                Id = sp500Id,
                OwnerUserId = GuestOwnerUserId,
                Name = "ETF S&P 500",
                Symbol = "ESE",
                Isin = "FR0011550185",
                Type = AssetType.Etf,
                Currency = "EUR",
                Country = "Etats-Unis",
                PriceProvider = "demo",
                MarketSymbol = "ESE.PA",
                StrategicStatus = StrategicStatus.Core
            },
            new Asset
            {
                Id = europeId,
                OwnerUserId = GuestOwnerUserId,
                Name = "ETF Europe",
                Symbol = "PCEU",
                Isin = "LU1681042609",
                Type = AssetType.Etf,
                Currency = "EUR",
                Country = "Europe",
                PriceProvider = "demo",
                MarketSymbol = "PCEU.PA",
                StrategicStatus = StrategicStatus.Conviction
            },
            new Asset
            {
                Id = energyId,
                OwnerUserId = GuestOwnerUserId,
                Name = "TotalEnergies",
                Symbol = "TTE",
                Isin = "FR0000120271",
                Type = AssetType.Stock,
                Currency = "EUR",
                Country = "France",
                PriceProvider = "demo",
                MarketSymbol = "TTE.PA",
                StrategicStatus = StrategicStatus.Observation
            }
        };

        dbContext.Portfolios.AddRange(portfolios);
        dbContext.Assets.AddRange(assets);
        dbContext.Repartitions.AddRange(
            AssetRepartition(peaId, worldId, 0.62m, 0.55m, 0.70m),
            AssetRepartition(peaId, europeId, 0.23m, 0.15m, 0.30m),
            AssetRepartition(peaId, energyId, 0.15m, 0.00m, 0.20m),
            AssetRepartition(ctoId, sp500Id, 0.80m, 0.65m, 0.90m),
            AssetRepartition(ctoId, energyId, 0.20m, 0.00m, 0.25m));
        dbContext.Transactions.AddRange(
            Buy(peaId, worldId, start.AddDays(5), 18m, 68.20m, "Achat initial MSCI World"),
            Buy(peaId, europeId, start.AddDays(12), 14m, 51.40m, "Diversification Europe"),
            Buy(ctoId, sp500Id, start.AddDays(25), 7m, 91.80m, "Exposition US"),
            Buy(peaId, worldId, start.AddDays(48), 5m, 71.10m, "Renforcement mensuel"),
            Buy(peaId, energyId, start.AddDays(63), 9m, 57.60m, "Ligne de conviction"),
            Buy(ctoId, sp500Id, start.AddDays(90), 3m, 96.20m, "Renforcement US"),
            Sell(peaId, energyId, start.AddDays(126), 2m, 62.30m, "Allegement partiel"),
            Buy(peaId, worldId, start.AddDays(145), 4m, 76.40m, "Renforcement sur repli"));
        dbContext.AssetPrices.AddRange(BuildDemoPrices(today, worldId, 68.20m, 81.40m, 1.25m));
        dbContext.AssetPrices.AddRange(BuildDemoPrices(today, sp500Id, 91.80m, 108.30m, 1.75m));
        dbContext.AssetPrices.AddRange(BuildDemoPrices(today, europeId, 51.40m, 57.90m, 0.95m));
        dbContext.AssetPrices.AddRange(BuildDemoPrices(today, energyId, 57.60m, 61.10m, 2.60m));
        dbContext.StrategyRules.AddRange(
            DemoRule(
                "Surveiller les ecarts d'allocation",
                "Alerte lorsqu'une ligne s'eloigne trop de sa cible.",
                "Ecart a la cible superieur a 5 points.",
                "Verifier si le prochain investissement doit corriger l'ecart.",
                RuleDefinition(
                    new RuleTargetDto(RuleTargetType.Position, RuleTargetMode.All, null, null),
                    new RuleConditionDto(RuleConditionMetric.AllocationDrift, RuleComparisonOperator.GreaterThanOrEqual, 0.05m, null, RuleValueUnit.PercentPoint, null),
                    new RuleEffectDto(RuleEffectType.RequireReview, RuleEffectStrength.Soft, RuleSeverity.Warning, "Ecart d'allocation a surveiller.")),
                10),
            DemoRule(
                "Prioriser le coeur de portefeuille",
                "Le PEA doit rester pilote par les ETF coeur.",
                "Regle structurelle active.",
                "Favoriser les renforcements sur les actifs coeur avant les convictions.",
                RuleDefinition(
                    new RuleTargetDto(RuleTargetType.Position, RuleTargetMode.PortfolioAssets, peaId, null),
                    new RuleConditionDto(RuleConditionMetric.Always, RuleComparisonOperator.Equal, null, null, RuleValueUnit.None, null),
                    new RuleEffectDto(RuleEffectType.PrioritizeBuy, RuleEffectStrength.Soft, RuleSeverity.Info, "Prioriser les lignes coeur du PEA.")),
                20),
            DemoRule(
                "Prendre du recul apres forte hausse",
                "Evite de renforcer automatiquement une ligne deja tres en gain latent.",
                "Gain latent superieur a 20 %.",
                "Limiter les achats et verifier le poids de la ligne avant d'investir.",
                RuleDefinition(
                    new RuleTargetDto(RuleTargetType.Position, RuleTargetMode.All, null, null),
                    new RuleConditionDto(RuleConditionMetric.UnrealizedGainPercent, RuleComparisonOperator.GreaterThanOrEqual, 0.20m, null, RuleValueUnit.Percent, null),
                    new RuleEffectDto(RuleEffectType.ReduceBuy, RuleEffectStrength.Soft, RuleSeverity.Warning, "Ligne en forte plus-value, renforcement a moderer.")),
                30));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Repartition PortfolioRepartition(Guid portfolioId, decimal targetWeight) => new()
    {
        OwnerUserId = GuestOwnerUserId,
        PortfolioId = portfolioId,
        Kind = RepartitionKind.Portfolio,
        TargetWeight = targetWeight
    };

    private static Repartition AssetRepartition(Guid portfolioId, Guid assetId, decimal targetWeight, decimal minWeight, decimal maxWeight) => new()
    {
        OwnerUserId = GuestOwnerUserId,
        PortfolioId = portfolioId,
        AssetId = assetId,
        Kind = RepartitionKind.PortfolioAsset,
        TargetWeight = targetWeight,
        MinWeight = minWeight,
        MaxWeight = maxWeight,
        Status = RepartitionStatus.Active
    };

    private static Transaction Buy(Guid portfolioId, Guid assetId, DateOnly date, decimal quantity, decimal unitPrice, string comment) => new()
    {
        OwnerUserId = GuestOwnerUserId,
        PortfolioId = portfolioId,
        AssetId = assetId,
        Type = TransactionType.Buy,
        Date = date,
        Quantity = quantity,
        UnitPrice = unitPrice,
        Fees = 1m,
        Currency = "EUR",
        Comment = comment
    };

    private static Transaction Sell(Guid portfolioId, Guid assetId, DateOnly date, decimal quantity, decimal unitPrice, string comment) => new()
    {
        OwnerUserId = GuestOwnerUserId,
        PortfolioId = portfolioId,
        AssetId = assetId,
        Type = TransactionType.Sell,
        Date = date,
        Quantity = quantity,
        UnitPrice = unitPrice,
        Fees = 1m,
        Currency = "EUR",
        Comment = comment
    };

    private static IEnumerable<AssetPrice> BuildDemoPrices(DateOnly today, Guid assetId, decimal startPrice, decimal endPrice, decimal wave)
    {
        const int days = 170;
        var start = today.AddDays(-days);
        for (var index = 0; index <= days; index++)
        {
            var progress = index / (decimal)days;
            var trend = startPrice + (endPrice - startPrice) * progress;
            var oscillation = (decimal)Math.Sin(index / 8d) * wave;
            var close = decimal.Round(trend + oscillation, 4, MidpointRounding.AwayFromZero);

            yield return new AssetPrice
            {
                OwnerUserId = GuestOwnerUserId,
                AssetId = assetId,
                Date = start.AddDays(index),
                Open = close * 0.997m,
                High = close * 1.006m,
                Low = close * 0.992m,
                Close = close,
                Currency = "EUR",
                Source = "tradecopilot-demo",
                RetrievedAt = DateTimeOffset.UtcNow.AddDays(-days + index)
            };
        }
    }

    private static StrategyRule DemoRule(
        string name,
        string description,
        string triggerCondition,
        string recommendedAction,
        RuleDefinitionDto definition,
        int priority) => new()
        {
            OwnerUserId = GuestOwnerUserId,
            Name = name,
            Description = description,
            TriggerCondition = triggerCondition,
            RecommendedAction = recommendedAction,
            DefinitionJson = JsonSerializer.Serialize(definition, RuleJsonOptions),
            Priority = priority,
            IsActive = true
        };

    private static RuleDefinitionDto RuleDefinition(
        RuleTargetDto target,
        RuleConditionDto condition,
        RuleEffectDto effect) => new(1, target, condition, effect);
}
