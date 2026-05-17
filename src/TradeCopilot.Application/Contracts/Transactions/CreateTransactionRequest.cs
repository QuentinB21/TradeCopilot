using TradeCopilot.Domain;

namespace TradeCopilot.Application.Contracts.Transactions;

public sealed record CreateTransactionRequest(
    Guid PortfolioId,
    Guid? AssetId,
    TransactionType Type,
    DateOnly Date,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fees,
    string Currency,
    string? Comment);
