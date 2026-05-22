using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Repartitions;

public sealed record UpdateRepartitionRequest(
    decimal TargetWeight,
    decimal? MinWeight,
    decimal? MaxWeight,
    RepartitionStatus Status);
