namespace TradeCopilot.Application.Contracts.Strategy;

public sealed record CreateStrategyRuleRequest(
    Guid? PortfolioId,
    Guid? AssetId,
    string Name,
    string Description,
    string? TriggerCondition,
    string RecommendedAction,
    RuleDefinitionDto? Definition,
    int Priority,
    bool IsActive);
