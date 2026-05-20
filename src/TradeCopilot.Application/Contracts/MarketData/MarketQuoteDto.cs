namespace TradeCopilot.Application.Contracts.MarketData;

public sealed record MarketQuoteDto(
    string Symbol,
    DateOnly Date,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal Close,
    string Currency,
    string Provider,
    DateTimeOffset RetrievedAt);
