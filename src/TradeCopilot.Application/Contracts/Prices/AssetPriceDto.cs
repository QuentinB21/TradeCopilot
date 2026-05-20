namespace TradeCopilot.Application.Contracts.Prices;

public sealed record AssetPriceDto(
    Guid Id,
    Guid AssetId,
    DateOnly Date,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal Close,
    string Currency,
    string Source,
    DateTimeOffset RetrievedAt);
