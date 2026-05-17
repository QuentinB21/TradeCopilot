namespace TradeCopilot.Domain;

public sealed class AllocationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }
    public decimal TargetWeight { get; set; }
    public decimal? MinWeight { get; set; }
    public decimal? MaxWeight { get; set; }
    public AllocationRuleStatus Status { get; set; }
}
