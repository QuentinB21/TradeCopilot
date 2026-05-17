using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Assets;

public sealed record UpdateAssetRequest(
    string Name,
    string Symbol,
    string? Isin,
    AssetType Type,
    string Currency,
    string? Sector,
    string? Country,
    string? PriceProvider,
    StrategicStatus StrategicStatus);
