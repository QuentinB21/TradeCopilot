using Microsoft.Extensions.DependencyInjection;
using TradeCopilot.Application.Services.Assets;
using TradeCopilot.Application.Services.Repartitions;
using TradeCopilot.Application.Services.Dashboard;
using TradeCopilot.Application.Services.Imports;
using TradeCopilot.Application.Services.InvestmentPlans;
using TradeCopilot.Application.Services.MarketData;
using TradeCopilot.Application.Services.Portfolios;
using TradeCopilot.Application.Services.Positions;
using TradeCopilot.Application.Services.Prices;
using TradeCopilot.Application.Services.Strategy;
using TradeCopilot.Application.Services.Transactions;

namespace TradeCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTradeCopilotApplication(this IServiceCollection services)
    {
        services.AddSingleton<PositionCalculator>();
        services.AddSingleton<DashboardService>();
        services.AddSingleton<MonthlyInvestmentPlanner>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IRepartitionService, RepartitionService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IPriceService, PriceService>();
        services.AddScoped<IPositionQueryService, PositionQueryService>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();
        services.AddScoped<IInvestmentPlanService, InvestmentPlanService>();
        services.AddScoped<IMarketDataService, MarketDataService>();
        services.AddScoped<IMarketPriceRefreshService, MarketPriceRefreshService>();
        services.AddScoped<IStrategyService, StrategyService>();
        services.AddScoped<IStrategyRuleService, StrategyRuleService>();
        services.AddScoped<ITransactionImportService, TransactionImportService>();
        services.AddScoped<ITransactionImportStrategy, TradeRepublicTransactionImportStrategy>();
        services.AddScoped<ITransactionImportStrategy, BoursobankTransactionImportStrategy>();

        return services;
    }
}
