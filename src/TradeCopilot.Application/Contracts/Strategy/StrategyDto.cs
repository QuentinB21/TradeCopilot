namespace TradeCopilot.Application.Contracts.Strategy;

public sealed record StrategyDto(
    IReadOnlyList<GlobalAllocationTargetDto> GlobalAllocation,
    IReadOnlyList<StrategyRuleDto> Rules);
