using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.InvestmentPlans;
using TradeCopilot.Application.Services.Rules;

namespace TradeCopilot.Application.Services.InvestmentPlans;

public sealed class InvestmentPlanService(
    IInvestmentRepository repository,
    PositionCalculator positionCalculator,
    MonthlyInvestmentPlanner monthlyInvestmentPlanner,
    RuleEvaluationService ruleEvaluationService) : IInvestmentPlanService
{
    public async Task<MonthlyInvestmentPlanDto> BuildMonthlyPlanAsync(MonthlyInvestmentRequest request, CancellationToken cancellationToken = default)
    {
        var portfolios = await repository.GetPortfoliosAsync(cancellationToken);
        var assets = await repository.GetAssetsAsync(cancellationToken);
        var transactions = await repository.GetTransactionsAsync(cancellationToken);
        var prices = await repository.GetPricesAsync(cancellationToken);
        var repartitions = await repository.GetAssetRepartitionsAsync(cancellationToken);
        var rules = await repository.GetStrategyRulesAsync(cancellationToken);
        var positions = positionCalculator.Calculate(portfolios, assets, transactions, prices, repartitions);
        var ruleSnapshot = ruleEvaluationService.Evaluate(rules, portfolios, assets, positions, prices);

        return monthlyInvestmentPlanner.BuildPlan(request.Amount, portfolios, assets, positions, repartitions, ruleSnapshot.AllocationImpacts);
    }
}
