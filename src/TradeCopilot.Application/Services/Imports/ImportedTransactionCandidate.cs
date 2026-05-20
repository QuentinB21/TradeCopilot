using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Imports;

public sealed record ImportedTransactionCandidate(
    int RowNumber,
    string ExternalId,
    TransactionType Type,
    DateOnly Date,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fees,
    string Currency,
    string Comment,
    ImportedAssetCandidate? Asset);
