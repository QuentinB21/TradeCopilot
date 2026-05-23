namespace TradeCopilot.Application.Contracts.Strategy;

public sealed record StrategyRuleDto(
    Guid Id,
    Guid? PortfolioId,
    Guid? AssetId,
    string Name,
    string Description,
    string? TriggerCondition,
    string RecommendedAction,
    RuleDefinitionDto? Definition,
    int Priority,
    bool IsActive);
