namespace TradeCopilot.Application.Contracts.Prices;

public sealed record UpdateAssetPriceRequest(
    Guid AssetId,
    DateOnly Date,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal Close,
    string Currency,
    string Source);
