using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace TradeCopilot.Infrastructure;

public sealed class DatabaseInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<TradeCopilotDbContext>();

        if (dbContext is null)
        {
            return;
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureCompatibleSchemaAsync(dbContext, cancellationToken);

        if (await dbContext.Portfolios.AnyAsync(cancellationToken))
        {
            await EnsureSeedConfigurationAsync(dbContext, cancellationToken);

            if (!await dbContext.StrategyRules.AnyAsync(cancellationToken))
            {
                var strategySeed = TradeCopilotSeedData.Create();
                dbContext.StrategyRules.AddRange(strategySeed.StrategyRules);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var seed = TradeCopilotSeedData.Create();
        dbContext.Portfolios.AddRange(seed.Portfolios);
        dbContext.Assets.AddRange(seed.Assets);
        dbContext.Transactions.AddRange(seed.Transactions);
        dbContext.AssetPrices.AddRange(seed.Prices);
        dbContext.AllocationRules.AddRange(seed.AllocationRules);
        dbContext.StrategyRules.AddRange(seed.StrategyRules);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureCompatibleSchemaAsync(TradeCopilotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsNpgsql())
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Portfolios" ADD COLUMN IF NOT EXISTS "TargetWeight" numeric(9,6) NOT NULL DEFAULT 0;
                UPDATE "Portfolios"
                SET "TargetWeight" = 0.80
                WHERE "Id" = '10000000-0000-0000-0000-000000000001' AND "TargetWeight" = 0;
                UPDATE "Portfolios"
                SET "TargetWeight" = 0.20
                WHERE "Id" = '10000000-0000-0000-0000-000000000002' AND "TargetWeight" = 0;
                CREATE TABLE IF NOT EXISTS "StrategyRules" (
                    "Id" uuid NOT NULL,
                    "PortfolioId" uuid NULL,
                    "AssetId" uuid NULL,
                    "Name" character varying(160) NOT NULL,
                    "Description" character varying(1200) NOT NULL,
                    "TriggerCondition" character varying(800) NULL,
                    "RecommendedAction" character varying(1200) NOT NULL,
                    "Priority" integer NOT NULL,
                    "IsActive" boolean NOT NULL,
                    CONSTRAINT "PK_StrategyRules" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_StrategyRules_Portfolios_PortfolioId" FOREIGN KEY ("PortfolioId") REFERENCES "Portfolios" ("Id"),
                    CONSTRAINT "FK_StrategyRules_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id")
                );
                CREATE INDEX IF NOT EXISTS "IX_StrategyRules_PortfolioId" ON "StrategyRules" ("PortfolioId");
                CREATE INDEX IF NOT EXISTS "IX_StrategyRules_AssetId" ON "StrategyRules" ("AssetId");
                """,
                cancellationToken);
        }
    }

    private static async Task EnsureSeedConfigurationAsync(TradeCopilotDbContext dbContext, CancellationToken cancellationToken)
    {
        var pea = await dbContext.Portfolios.FirstOrDefaultAsync(
            portfolio => portfolio.Id == Guid.Parse("10000000-0000-0000-0000-000000000001") && portfolio.TargetWeight == 0,
            cancellationToken);
        if (pea is not null)
        {
            pea.TargetWeight = 0.80m;
        }

        var tradeRepublic = await dbContext.Portfolios.FirstOrDefaultAsync(
            portfolio => portfolio.Id == Guid.Parse("10000000-0000-0000-0000-000000000002") && portfolio.TargetWeight == 0,
            cancellationToken);
        if (tradeRepublic is not null)
        {
            tradeRepublic.TargetWeight = 0.20m;
        }

        if (pea is not null || tradeRepublic is not null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
