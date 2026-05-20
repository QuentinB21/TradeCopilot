namespace TradeCopilot.Application.Contracts.Imports;

public sealed record TransactionImportRequest(
    TransactionImportProvider Provider,
    Guid PortfolioId,
    string FileName);
