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

        // The application must start empty; demo data belongs to tests only.
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
