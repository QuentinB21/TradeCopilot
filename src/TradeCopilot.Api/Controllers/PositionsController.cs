using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Positions;
using TradeCopilot.Application.Services.Positions;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/positions")]
public sealed class PositionsController(IPositionQueryService positionQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PositionDto>>> GetPositions(CancellationToken cancellationToken)
    {
        var positions = await positionQueryService.GetPositionsAsync(cancellationToken);
        return Ok(positions);
    }
}
