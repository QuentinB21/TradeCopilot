using TradeCopilot.Application.Contracts.Imports;

namespace TradeCopilot.Application.Services.Imports;

public interface ITransactionImportStrategy
{
    TransactionImportProvider Provider { get; }

    Task<TransactionImportParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
}
