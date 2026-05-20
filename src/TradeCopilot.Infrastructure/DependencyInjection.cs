using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradeCopilot.Application.Abstractions;
using TradeCopilot.Infrastructure.MarketData;
using TradeCopilot.Infrastructure.Persistence;

namespace TradeCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTradeCopilotInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TradeCopilot");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<TradeCopilotDbContext>(options => options.UseNpgsql(connectionString));
            services.AddHostedService<DatabaseInitializer>();
        }

        services.AddScoped<IInvestmentRepository, EfInvestmentRepository>();
        services.AddHttpClient<IMarketDataProvider, YahooFinanceMarketDataProvider>(client =>
        {
            client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TradeCopilot/0.1");
        });

        return services;
    }
}
