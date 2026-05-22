using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Repartitions;
using TradeCopilot.Application.Services.Repartitions;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/repartitions")]
public sealed class RepartitionsController(IRepartitionService repartitionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RepartitionDto>>> GetAssetRepartitions(CancellationToken cancellationToken)
    {
        var repartitions = await repartitionService.GetAssetRepartitionsAsync(cancellationToken);
        return Ok(repartitions);
    }

    [HttpPost]
    public async Task<ActionResult<RepartitionDto>> CreateAssetRepartition(CreateRepartitionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var repartition = await repartitionService.CreateAssetRepartitionAsync(request, cancellationToken);
            return Created($"/api/repartitions/{repartition.Id}", repartition);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RepartitionDto>> UpdateAssetRepartition(Guid id, UpdateRepartitionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var repartition = await repartitionService.UpdateAssetRepartitionAsync(id, request, cancellationToken);
            return repartition is null ? NotFound() : Ok(repartition);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAssetRepartition(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repartitionService.DeleteAssetRepartitionAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
