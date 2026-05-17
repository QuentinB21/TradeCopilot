using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Transactions;
using TradeCopilot.Application.Services.Transactions;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetTransactions(CancellationToken cancellationToken)
    {
        var transactions = await transactionService.GetTransactionsAsync(cancellationToken);
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = await transactionService.CreateTransactionAsync(request, cancellationToken);
        return Created($"/api/transactions/{transaction.Id}", transaction);
    }
}
