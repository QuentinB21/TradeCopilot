namespace TradeCopilot.Domain;

public sealed class Portfolio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public PortfolioType Type { get; set; }
    public required string Broker { get; set; }
    public required string BaseCurrency { get; set; }
    public decimal CashBalance { get; set; }
    public decimal TargetWeight { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Transaction> Transactions { get; set; } = [];
    public List<AllocationRule> AllocationRules { get; set; } = [];
}
