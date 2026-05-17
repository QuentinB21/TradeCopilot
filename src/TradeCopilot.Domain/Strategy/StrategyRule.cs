namespace TradeCopilot.Domain;

public sealed class StrategyRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? TriggerCondition { get; set; }
    public required string RecommendedAction { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
}
