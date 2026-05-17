using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Allocation;

public sealed record UpdateAllocationRuleRequest(
    decimal TargetWeight,
    decimal? MinWeight,
    decimal? MaxWeight,
    AllocationRuleStatus Status);
