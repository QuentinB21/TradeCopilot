using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Positions;

public sealed record PositionDto(
    Guid PortfolioId,
    string PortfolioName,
    Guid AssetId,
    string AssetName,
    string Symbol,
    StrategicStatus StrategicStatus,
    decimal Quantity,
    decimal AverageBuyPrice,
    decimal InvestedAmount,
    decimal MarketPrice,
    decimal MarketValue,
    decimal UnrealizedGain,
    decimal UnrealizedGainPercent,
    decimal RealizedGain,
    decimal Weight,
    decimal? TargetWeight,
    decimal? AllocationDrift);
