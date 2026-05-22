namespace TradeCopilot.Domain;

public sealed class Asset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Symbol { get; set; }
    public string? Isin { get; set; }
    public AssetType Type { get; set; }
    public required string Currency { get; set; }
    public string? Country { get; set; }
    public string? PriceProvider { get; set; }
    public string? MarketSymbol { get; set; }
    public StrategicStatus StrategicStatus { get; set; }
    public List<AssetPrice> Prices { get; set; } = [];
}
