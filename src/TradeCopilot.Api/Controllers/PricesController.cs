using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Prices;
using TradeCopilot.Application.Services.Prices;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/prices")]
public sealed class PricesController(IPriceService priceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetPriceDto>>> GetPrices(CancellationToken cancellationToken)
    {
        var prices = await priceService.GetPricesAsync(cancellationToken);
        return Ok(prices);
    }

    [HttpPost]
    public async Task<ActionResult<AssetPriceDto>> CreatePrice(CreateAssetPriceRequest request, CancellationToken cancellationToken)
    {
        var price = await priceService.CreatePriceAsync(request, cancellationToken);
        return Created($"/api/prices/{price.Id}", price);
    }
}
