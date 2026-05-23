using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Application.Abstractions;

namespace TradeCopilot.Application.Services.Strategy;

public sealed class StrategyService(IInvestmentRepository repository) : IStrategyService
{
    public async Task<StrategyDto> GetStrategyAsync(CancellationToken cancellationToken = default)
    {
        var portfolios = await repository.GetPortfoliosAsync(cancellationToken);
        var rules = await repository.GetStrategyRulesAsync(cancellationToken);

        return new StrategyDto(
            portfolios
                .OrderBy(portfolio => portfolio.Name)
                .Select(portfolio => new GlobalAllocationTargetDto(portfolio.Name, portfolio.TargetWeight))
                .ToList(),
            rules
                .OrderBy(rule => rule.Priority)
                .ThenBy(rule => rule.Name)
                .Select(rule => new StrategyRuleDto(
                    rule.Id,
                    rule.PortfolioId,
                    rule.AssetId,
                    rule.Name,
                    rule.Description,
                    rule.TriggerCondition,
                    rule.RecommendedAction,
                    StrategyRuleService.DeserializeDefinition(rule.DefinitionJson),
                    rule.Priority,
                    rule.IsActive))
                .ToList());
    }
}
