using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Portfolios;

public sealed record PortfolioDto(
    Guid Id,
    string Name,
    PortfolioType Type,
    string Broker,
    string BaseCurrency,
    decimal CashBalance);
