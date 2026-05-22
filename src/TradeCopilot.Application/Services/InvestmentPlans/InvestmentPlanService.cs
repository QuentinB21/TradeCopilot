using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.InvestmentPlans;

namespace TradeCopilot.Application.Services.InvestmentPlans;

public sealed class InvestmentPlanService(
    IInvestmentRepository repository,
    PositionCalculator positionCalculator,
    MonthlyInvestmentPlanner monthlyInvestmentPlanner) : IInvestmentPlanService
{
    public async Task<MonthlyInvestmentPlanDto> BuildMonthlyPlanAsync(MonthlyInvestmentRequest request, CancellationToken cancellationToken = default)
    {
        var portfolios = await repository.GetPortfoliosAsync(cancellationToken);
        var assets = await repository.GetAssetsAsync(cancellationToken);
        var transactions = await repository.GetTransactionsAsync(cancellationToken);
        var prices = await repository.GetPricesAsync(cancellationToken);
        var repartitions = await repository.GetAssetRepartitionsAsync(cancellationToken);
        var positions = positionCalculator.Calculate(portfolios, assets, transactions, prices, repartitions);

        return monthlyInvestmentPlanner.BuildPlan(request.Amount, portfolios, assets, positions, repartitions);
    }
}
