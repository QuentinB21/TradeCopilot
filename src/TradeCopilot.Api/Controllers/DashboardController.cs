using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Dashboard;
using TradeCopilot.Application.Services.Dashboard;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardQueryService dashboardQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await dashboardQueryService.GetDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }
}
