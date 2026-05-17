namespace TradeCopilot.Domain;

public sealed class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ReportPeriodType PeriodType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public required string ContentJson { get; set; }
}
