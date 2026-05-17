using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        if (await dbContext.Portfolios.AnyAsync(cancellationToken))
        {
            return;
        }

        var seed = TradeCopilotSeedData.Create();
        dbContext.Portfolios.AddRange(seed.Portfolios);
        dbContext.Assets.AddRange(seed.Assets);
        dbContext.Transactions.AddRange(seed.Transactions);
        dbContext.AssetPrices.AddRange(seed.Prices);
        dbContext.AllocationRules.AddRange(seed.AllocationRules);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
