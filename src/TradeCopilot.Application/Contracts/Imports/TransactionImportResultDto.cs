namespace TradeCopilot.Application.Contracts.Imports;

public sealed record TransactionImportResultDto(
    int RowsRead,
    int ImportedTransactions,
    int CreatedAssets,
    int DuplicateRows,
    int SkippedRows,
    IReadOnlyList<ImportWarningDto> Warnings);
