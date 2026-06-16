using System.ComponentModel.DataAnnotations.Schema;

namespace TradeCopilot.Domain;

public sealed class Portfolio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerUserId { get; set; } = string.Empty;
    public required string Name { get; set; }
    public PortfolioType Type { get; set; }
    public required string Broker { get; set; }
    public required string BaseCurrency { get; set; }
    public decimal CashBalance { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Transaction> Transactions { get; set; } = [];
    public List<Repartition> Repartitions { get; set; } = [];

    [NotMapped]
    public decimal TargetWeight => Repartitions
        .FirstOrDefault(repartition => repartition.Kind == RepartitionKind.Portfolio)
        ?.TargetWeight ?? 0m;
}
