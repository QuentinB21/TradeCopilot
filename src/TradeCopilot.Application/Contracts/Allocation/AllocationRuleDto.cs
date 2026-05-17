using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Allocation;

public sealed record AllocationRuleDto(
    Guid Id,
    Guid PortfolioId,
    Guid AssetId,
    decimal TargetWeight,
    decimal? MinWeight,
    decimal? MaxWeight,
    AllocationRuleStatus Status);
