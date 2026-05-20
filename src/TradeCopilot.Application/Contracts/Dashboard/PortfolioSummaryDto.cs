namespace TradeCopilot.Application.Contracts.Dashboard;

public sealed record PortfolioSummaryDto(
    Guid PortfolioId,
    string Name,
    decimal MarketValue,
    decimal InvestedAmount,
    decimal UnrealizedGain,
    decimal UnrealizedGainPercent,
    decimal TargetWeight,
    decimal ActualWeight,
    decimal AllocationDrift);
