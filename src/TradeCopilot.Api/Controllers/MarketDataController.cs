using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.MarketData;
using TradeCopilot.Application.Services.MarketData;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/market-data")]
public sealed class MarketDataController(IMarketDataService marketDataService) : ControllerBase
{
    [HttpGet("instruments")]
    public async Task<ActionResult<IReadOnlyList<InstrumentSearchResultDto>>> SearchInstruments([FromQuery] string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Le critere de recherche est obligatoire.");
        }

        var results = await marketDataService.SearchInstrumentsAsync(query, cancellationToken);
        return Ok(results);
    }

    [HttpGet("quotes/{symbol}")]
    public async Task<ActionResult<MarketQuoteDto>> GetLatestQuote(string symbol, CancellationToken cancellationToken)
    {
        var quote = await marketDataService.GetLatestQuoteAsync(symbol, cancellationToken);
        return quote is null ? NotFound() : Ok(quote);
    }
}
