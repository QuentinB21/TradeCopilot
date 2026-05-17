using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Application.Services.Strategy;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/strategy")]
public sealed class StrategyController(IStrategyService strategyService) : ControllerBase
{
    [HttpGet]
    public ActionResult<StrategyDto> GetStrategy()
    {
        return Ok(strategyService.GetStrategy());
    }
}
