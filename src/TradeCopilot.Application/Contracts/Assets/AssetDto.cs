using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Assets;

public sealed record AssetDto(
    Guid Id,
    string Name,
    string Symbol,
    string? Isin,
    AssetType Type,
    string Currency,
    string? PriceProvider,
    string? MarketSymbol,
    StrategicStatus StrategicStatus);
