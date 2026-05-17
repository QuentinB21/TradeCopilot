using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Common;
using TradeCopilot.Application.Contracts.Portfolios;
using TradeCopilot.Application.Services.Portfolios;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/portfolios")]
public sealed class PortfoliosController(IPortfolioService portfolioService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PortfolioDto>>> GetPortfolios(CancellationToken cancellationToken)
    {
        var portfolios = await portfolioService.GetPortfoliosAsync(cancellationToken);
        return Ok(portfolios);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PortfolioDto>> GetPortfolio(Guid id, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioService.GetPortfolioAsync(id, cancellationToken);
        return portfolio is null ? NotFound() : Ok(portfolio);
    }

    [HttpPost]
    public async Task<ActionResult<PortfolioDto>> CreatePortfolio(CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioService.CreatePortfolioAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPortfolio), new { id = portfolio.Id }, portfolio);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PortfolioDto>> UpdatePortfolio(Guid id, UpdatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioService.UpdatePortfolioAsync(id, request, cancellationToken);
        return portfolio is null ? NotFound() : Ok(portfolio);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePortfolio(Guid id, CancellationToken cancellationToken)
    {
        var result = await portfolioService.DeletePortfolioAsync(id, cancellationToken);
        return result switch
        {
            DeleteEntityResult.Deleted => NoContent(),
            DeleteEntityResult.NotFound => NotFound(),
            DeleteEntityResult.Conflict => Conflict("Ce portefeuille est reference par des transactions ou des regles. Supprimez d'abord ces dependances."),
            _ => Problem()
        };
    }
}
