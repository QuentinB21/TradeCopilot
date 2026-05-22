using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Assets;

public sealed record CreateAssetRequest(
    string Name,
    string Symbol,
    string? Isin,
    AssetType Type,
    string Currency,
    string? Country,
    string? PriceProvider,
    string? MarketSymbol,
    StrategicStatus StrategicStatus);
