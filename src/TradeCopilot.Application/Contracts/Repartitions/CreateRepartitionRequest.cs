using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Repartitions;

public sealed record CreateRepartitionRequest(
    Guid PortfolioId,
    Guid AssetId,
    decimal TargetWeight,
    decimal? MinWeight,
    decimal? MaxWeight,
    RepartitionStatus Status);
