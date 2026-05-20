using TradeCopilot.Application.Contracts.Imports;

namespace TradeCopilot.Application.Services.Imports;

public interface ITransactionImportService
{
    Task<TransactionImportResultDto?> ImportAsync(
        TransactionImportRequest request,
        Stream stream,
        CancellationToken cancellationToken = default);
}
