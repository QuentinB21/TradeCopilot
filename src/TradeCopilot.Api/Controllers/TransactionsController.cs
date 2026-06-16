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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await transactionService.GetTransactionAsync(id, cancellationToken);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await transactionService.CreateTransactionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> UpdateTransaction(Guid id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await transactionService.UpdateTransactionAsync(id, request, cancellationToken);
            return transaction is null ? NotFound() : Ok(transaction);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await transactionService.DeleteTransactionAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
