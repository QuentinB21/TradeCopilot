using TradeCopilot.Application.Contracts.Transactions;

namespace TradeCopilot.Application.Services.Transactions;

public interface ITransactionService
{
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default);
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default);
}
