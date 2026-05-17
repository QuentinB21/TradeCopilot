namespace TradeCopilot.Application.Contracts.InvestmentPlans;

public sealed record MonthlyInvestmentPlanDto(
    decimal Amount,
    IReadOnlyList<InvestmentEnvelopeRecommendationDto> Envelopes,
    IReadOnlyList<string> Notes);
