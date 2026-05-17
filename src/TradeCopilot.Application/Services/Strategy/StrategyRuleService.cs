using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Strategy;

public sealed class StrategyRuleService(IInvestmentRepository repository) : IStrategyRuleService
{
    public async Task<IReadOnlyList<StrategyRuleDto>> GetStrategyRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await repository.GetStrategyRulesAsync(cancellationToken);
        return rules.OrderBy(rule => rule.Priority).ThenBy(rule => rule.Name).Select(ToDto).ToList();
    }

    public async Task<StrategyRuleDto> CreateStrategyRuleAsync(CreateStrategyRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = new StrategyRule
        {
            PortfolioId = request.PortfolioId,
            AssetId = request.AssetId,
            Name = NormalizeRequired(request.Name),
            Description = NormalizeRequired(request.Description),
            TriggerCondition = NormalizeOptional(request.TriggerCondition),
            RecommendedAction = NormalizeRequired(request.RecommendedAction),
            Priority = request.Priority,
            IsActive = request.IsActive
        };

        await repository.AddStrategyRuleAsync(rule, cancellationToken);
        return ToDto(rule);
    }

    public async Task<StrategyRuleDto?> UpdateStrategyRuleAsync(Guid id, UpdateStrategyRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetStrategyRuleByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        rule.PortfolioId = request.PortfolioId;
        rule.AssetId = request.AssetId;
        rule.Name = NormalizeRequired(request.Name);
        rule.Description = NormalizeRequired(request.Description);
        rule.TriggerCondition = NormalizeOptional(request.TriggerCondition);
        rule.RecommendedAction = NormalizeRequired(request.RecommendedAction);
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;

        await repository.UpdateStrategyRuleAsync(rule, cancellationToken);
        return ToDto(rule);
    }

    public async Task<bool> DeleteStrategyRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetStrategyRuleByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        await repository.DeleteStrategyRuleAsync(rule, cancellationToken);
        return true;
    }

    private static StrategyRuleDto ToDto(StrategyRule rule) => new(
        rule.Id,
        rule.PortfolioId,
        rule.AssetId,
        rule.Name,
        rule.Description,
        rule.TriggerCondition,
        rule.RecommendedAction,
        rule.Priority,
        rule.IsActive);

    private static string NormalizeRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
