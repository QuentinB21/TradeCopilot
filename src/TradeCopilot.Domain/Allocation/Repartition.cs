namespace TradeCopilot.Domain;

public sealed class Repartition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RepartitionKind Kind { get; set; }
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public decimal TargetWeight { get; set; }
    public decimal? MinWeight { get; set; }
    public decimal? MaxWeight { get; set; }
    public RepartitionStatus? Status { get; set; }
}
