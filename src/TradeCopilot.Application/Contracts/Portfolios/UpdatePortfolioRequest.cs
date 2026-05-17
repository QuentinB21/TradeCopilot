using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Portfolios;

public sealed record UpdatePortfolioRequest(
    string Name,
    PortfolioType Type,
    string Broker,
    string BaseCurrency,
    decimal CashBalance,
    decimal TargetWeight);
