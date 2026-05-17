namespace TradeCopilot.Application.Contracts.InvestmentPlans;

public sealed record InvestmentEnvelopeRecommendationDto(
    Guid PortfolioId,
    string PortfolioName,
    decimal Amount,
    IReadOnlyList<InvestmentLineRecommendationDto> Lines);
