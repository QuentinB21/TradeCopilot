using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Transactions;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Transactions;

public sealed class TransactionService(IInvestmentRepository repository) : ITransactionService
{
    public async Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await repository.GetTransactionsAsync(cancellationToken);
        return transactions
            .OrderByDescending(transaction => transaction.Date)
            .Select(ToDto)
            .ToList();
    }

    public async Task<TransactionDto?> GetTransactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetTransactionByIdAsync(id, cancellationToken);
        return transaction is null ? null : ToDto(transaction);
    }

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Currency);

        var transaction = new Transaction
        {
            PortfolioId = request.PortfolioId,
            AssetId = request.AssetId,
            Type = request.Type,
            Date = request.Date,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            Fees = request.Fees,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Comment = request.Comment?.Trim()
        };

        await repository.AddTransactionAsync(transaction, cancellationToken);
        return ToDto(transaction);
    }

    public async Task<TransactionDto?> UpdateTransactionAsync(Guid id, UpdateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Currency);

        var transaction = await repository.GetTransactionByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        transaction.PortfolioId = request.PortfolioId;
        transaction.AssetId = request.AssetId;
        transaction.Type = request.Type;
        transaction.Date = request.Date;
        transaction.Quantity = request.Quantity;
        transaction.UnitPrice = request.UnitPrice;
        transaction.Fees = request.Fees;
        transaction.Currency = request.Currency.Trim().ToUpperInvariant();
        transaction.Comment = request.Comment?.Trim();

        await repository.UpdateTransactionAsync(transaction, cancellationToken);
        return ToDto(transaction);
    }

    public async Task<bool> DeleteTransactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetTransactionByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        await repository.DeleteTransactionAsync(transaction, cancellationToken);
        return true;
    }

    private static TransactionDto ToDto(Transaction transaction) => new(
        transaction.Id,
        transaction.PortfolioId,
        transaction.AssetId,
        transaction.Type,
        transaction.Date,
        transaction.Quantity,
        transaction.UnitPrice,
        transaction.Fees,
        transaction.Currency,
        transaction.Comment,
        transaction.ImportSource,
        transaction.ExternalId);
}
