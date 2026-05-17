namespace TradeCopilot.Domain;

public sealed class InvestmentJournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public required string DecisionType { get; set; }
    public required string Context { get; set; }
    public required string Decision { get; set; }
    public required string Rationale { get; set; }
    public string? Emotion { get; set; }
    public string? FollowUpResult { get; set; }
}
