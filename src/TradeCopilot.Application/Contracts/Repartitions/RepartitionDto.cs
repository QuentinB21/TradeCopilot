using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Repartitions;

public sealed record RepartitionDto(
    Guid Id,
    Guid PortfolioId,
    Guid AssetId,
    decimal TargetWeight,
    decimal? MinWeight,
    decimal? MaxWeight,
    RepartitionStatus Status);
