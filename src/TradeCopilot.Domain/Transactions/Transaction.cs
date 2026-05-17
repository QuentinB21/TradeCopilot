namespace TradeCopilot.Domain;

public sealed class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public TransactionType Type { get; set; }
    public DateOnly Date { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Fees { get; set; }
    public required string Currency { get; set; }
    public string? Comment { get; set; }
}
