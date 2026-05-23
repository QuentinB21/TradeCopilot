using TradeCopilot.Application.Contracts.Positions;
using TradeCopilot.Application.Contracts.Rules;

namespace TradeCopilot.Application.Contracts.Dashboard;

public sealed record DashboardDto(
    decimal TotalMarketValue,
    decimal TotalInvested,
    decimal TotalUnrealizedGain,
    decimal TotalUnrealizedGainPercent,
    IReadOnlyList<PortfolioSummaryDto> Portfolios,
    IReadOnlyList<PositionDto> Positions,
    IReadOnlyList<DashboardHistoryPointDto> History,
    IReadOnlyList<RuleAlertDto> RuleAlerts);

public sealed record DashboardHistoryPointDto(
    DateOnly Date,
    decimal TotalMarketValue,
    decimal TotalInvested,
    decimal TotalUnrealizedGain,
    IReadOnlyList<PortfolioHistoryPointDto> Portfolios);

public sealed record PortfolioHistoryPointDto(
    Guid PortfolioId,
    string PortfolioName,
    decimal MarketValue,
    decimal InvestedAmount,
    decimal UnrealizedGain);
