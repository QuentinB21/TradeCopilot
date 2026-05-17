namespace TradeCopilot.Application.Contracts.Prices;

public sealed record CreateAssetPriceRequest(
    Guid AssetId,
    DateOnly Date,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal Close,
    string Currency,
    string Source);
