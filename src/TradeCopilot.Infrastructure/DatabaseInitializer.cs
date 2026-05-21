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

        // The application must start empty; demo data belongs to tests only.
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureCompatibleSchemaAsync(TradeCopilotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsNpgsql())
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Portfolios" ADD COLUMN IF NOT EXISTS "TargetWeight" numeric(9,6) NOT NULL DEFAULT 0;
                ALTER TABLE "Assets" ADD COLUMN IF NOT EXISTS "MarketSymbol" character varying(48) NULL;
                ALTER TABLE "AssetPrices" ADD COLUMN IF NOT EXISTS "RetrievedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '1970-01-01 00:00:00+00';
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssetPrices_AssetId_Date" ON "AssetPrices" ("AssetId", "Date");
                ALTER TABLE "Transactions" ADD COLUMN IF NOT EXISTS "ImportSource" character varying(80) NULL;
                ALTER TABLE "Transactions" ADD COLUMN IF NOT EXISTS "ExternalId" character varying(160) NULL;
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Transactions_ImportSource_ExternalId"
                    ON "Transactions" ("ImportSource", "ExternalId")
                    WHERE "ImportSource" IS NOT NULL AND "ExternalId" IS NOT NULL;
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
}
