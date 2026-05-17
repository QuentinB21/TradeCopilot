using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Allocation;
using TradeCopilot.Application.Services.Allocation;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/allocation-rules")]
public sealed class AllocationRulesController(IAllocationRuleService allocationRuleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AllocationRuleDto>>> GetAllocationRules(CancellationToken cancellationToken)
    {
        var rules = await allocationRuleService.GetAllocationRulesAsync(cancellationToken);
        return Ok(rules);
    }

    [HttpPost]
    public async Task<ActionResult<AllocationRuleDto>> CreateAllocationRule(CreateAllocationRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await allocationRuleService.CreateAllocationRuleAsync(request, cancellationToken);
        return Created($"/api/allocation-rules/{rule.Id}", rule);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AllocationRuleDto>> UpdateAllocationRule(Guid id, UpdateAllocationRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await allocationRuleService.UpdateAllocationRuleAsync(id, request, cancellationToken);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAllocationRule(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await allocationRuleService.DeleteAllocationRuleAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
