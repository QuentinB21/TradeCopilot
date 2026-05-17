namespace TradeCopilot.Application.Contracts.Strategy;

public sealed record StrategyDto(
    IReadOnlyList<GlobalAllocationTargetDto> GlobalAllocation,
    IReadOnlyList<string> PeaRules,
    IReadOnlyList<string> TradeRepublicRules);
