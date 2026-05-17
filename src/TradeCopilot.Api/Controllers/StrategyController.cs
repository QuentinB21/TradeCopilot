using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Application.Services.Strategy;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/strategy")]
public sealed class StrategyController(IStrategyService strategyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StrategyDto>> GetStrategy(CancellationToken cancellationToken)
    {
        return Ok(await strategyService.GetStrategyAsync(cancellationToken));
    }
}
