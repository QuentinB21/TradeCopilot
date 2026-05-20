using TradeCopilot.Application.Contracts.Transactions;

namespace TradeCopilot.Application.Services.Transactions;

public interface ITransactionService
{
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default);
    Task<TransactionDto?> GetTransactionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default);
    Task<TransactionDto?> UpdateTransactionAsync(Guid id, UpdateTransactionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteTransactionAsync(Guid id, CancellationToken cancellationToken = default);
}
