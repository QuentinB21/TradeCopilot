using TradeCopilot.Application.Contracts.Positions;

namespace TradeCopilot.Application.Contracts.Dashboard;

public sealed record DashboardDto(
    decimal TotalMarketValue,
    decimal TotalInvested,
    decimal TotalUnrealizedGain,
    decimal TotalUnrealizedGainPercent,
    IReadOnlyList<PortfolioSummaryDto> Portfolios,
    IReadOnlyList<PositionDto> Positions);
