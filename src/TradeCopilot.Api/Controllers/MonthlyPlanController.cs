using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.InvestmentPlans;
using TradeCopilot.Application.Services.InvestmentPlans;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/monthly-plan")]
public sealed class MonthlyPlanController(IInvestmentPlanService investmentPlanService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MonthlyInvestmentPlanDto>> BuildMonthlyPlan(MonthlyInvestmentRequest request, CancellationToken cancellationToken)
    {
        var plan = await investmentPlanService.BuildMonthlyPlanAsync(request, cancellationToken);
        return Ok(plan);
    }
}
