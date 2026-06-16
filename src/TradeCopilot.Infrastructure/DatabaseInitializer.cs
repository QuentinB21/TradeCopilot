using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace TradeCopilot.Infrastructure;

public sealed class DatabaseInitializer(IServiceProvider serviceProvider) : IHostedService
{
    private const string ExistingDataFallbackOwnerUserId = "local-development-user";

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

        // The application must start empty; demo data belongs to tests only.
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
}
