namespace TradeCopilot.Application.Contracts.Assets;

public sealed record BindMarketInstrumentRequest(
    string MarketSymbol,
    string? PriceProvider);
