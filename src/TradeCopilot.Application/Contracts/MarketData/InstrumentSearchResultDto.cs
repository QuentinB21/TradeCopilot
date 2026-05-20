using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.MarketData;

public sealed record InstrumentSearchResultDto(
    string Symbol,
    string Name,
    string? Exchange,
    string? ExchangeDisplay,
    string? QuoteType,
    string? Currency,
    string? Sector,
    AssetType SuggestedType,
    string Provider);
