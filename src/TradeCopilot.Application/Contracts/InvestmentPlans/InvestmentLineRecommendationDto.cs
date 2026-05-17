namespace TradeCopilot.Application.Contracts.InvestmentPlans;

public sealed record InvestmentLineRecommendationDto(
    Guid AssetId,
    string Symbol,
    string AssetName,
    decimal Amount,
    decimal TargetWeight,
    decimal? CurrentWeight,
    string Rationale);
