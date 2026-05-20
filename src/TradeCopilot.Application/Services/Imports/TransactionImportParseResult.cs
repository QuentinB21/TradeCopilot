using TradeCopilot.Application.Contracts.Imports;

namespace TradeCopilot.Application.Services.Imports;

public sealed record TransactionImportParseResult(
    int RowsRead,
    IReadOnlyList<ImportedTransactionCandidate> Transactions,
    IReadOnlyList<ImportWarningDto> Warnings);
