using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Application.Services.Strategy;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/strategy-rules")]
public sealed class StrategyRulesController(IStrategyRuleService strategyRuleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StrategyRuleDto>>> GetStrategyRules(CancellationToken cancellationToken)
    {
        var rules = await strategyRuleService.GetStrategyRulesAsync(cancellationToken);
        return Ok(rules);
    }

    [HttpGet("export")]
    public async Task<ActionResult<StrategyRulesExportDto>> ExportStrategyRules(CancellationToken cancellationToken)
    {
        var exportFile = await strategyRuleService.ExportStrategyRulesAsync(cancellationToken);
        return Ok(exportFile);
    }

    [HttpPost("import")]
    public async Task<ActionResult<StrategyRuleImportResultDto>> ImportStrategyRules(StrategyRulesExportDto importFile, CancellationToken cancellationToken)
    {
        try
        {
            var result = await strategyRuleService.ImportStrategyRulesAsync(importFile, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<StrategyRuleDto>> CreateStrategyRule(CreateStrategyRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await strategyRuleService.CreateStrategyRuleAsync(request, cancellationToken);
            return Created($"/api/strategy-rules/{rule.Id}", rule);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StrategyRuleDto>> UpdateStrategyRule(Guid id, UpdateStrategyRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await strategyRuleService.UpdateStrategyRuleAsync(id, request, cancellationToken);
            return rule is null ? NotFound() : Ok(rule);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStrategyRule(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await strategyRuleService.DeleteStrategyRuleAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
