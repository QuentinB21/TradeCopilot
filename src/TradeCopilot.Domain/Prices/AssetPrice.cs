namespace TradeCopilot.Domain;

public sealed class AssetPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerUserId { get; set; } = string.Empty;
    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }
    public DateOnly Date { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal Close { get; set; }
    public required string Currency { get; set; }
    public required string Source { get; set; }
    public DateTimeOffset RetrievedAt { get; set; } = DateTimeOffset.UtcNow;
}
