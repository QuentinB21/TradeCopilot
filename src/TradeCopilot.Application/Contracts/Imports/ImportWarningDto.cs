namespace TradeCopilot.Application.Contracts.Imports;

public sealed record ImportWarningDto(
    int? RowNumber,
    string Code,
    string Message,
    string Recommendation);
