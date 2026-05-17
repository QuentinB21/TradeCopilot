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
        transaction.Comment);
}
