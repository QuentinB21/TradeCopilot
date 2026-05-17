namespace TradeCopilot.Application.Contracts.Strategy;

public sealed record UpdateStrategyRuleRequest(
    Guid? PortfolioId,
    Guid? AssetId,
    string Name,
    string Description,
    string? TriggerCondition,
    string RecommendedAction,
    int Priority,
    bool IsActive);
