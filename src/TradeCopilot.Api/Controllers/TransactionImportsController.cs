using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Imports;
using TradeCopilot.Application.Services.Imports;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/transaction-imports")]
public sealed class TransactionImportsController(ITransactionImportService transactionImportService) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TransactionImportResultDto>> ImportTransactions(
        [FromForm] TransactionImportProvider provider,
        [FromForm] Guid portfolioId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("Le fichier CSV est vide.");
        }

        await using var stream = file.OpenReadStream();
        var result = await transactionImportService.ImportAsync(
            new TransactionImportRequest(provider, portfolioId, file.FileName),
            stream,
            cancellationToken);

        return result is null ? NotFound("Portefeuille introuvable.") : Ok(result);
    }
}
