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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetPriceDto>> GetPrice(Guid id, CancellationToken cancellationToken)
    {
        var price = await priceService.GetPriceAsync(id, cancellationToken);
        return price is null ? NotFound() : Ok(price);
    }

    [HttpPost]
    public async Task<ActionResult<AssetPriceDto>> CreatePrice(CreateAssetPriceRequest request, CancellationToken cancellationToken)
    {
        var price = await priceService.CreatePriceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPrice), new { id = price.Id }, price);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssetPriceDto>> UpdatePrice(Guid id, UpdateAssetPriceRequest request, CancellationToken cancellationToken)
    {
        var price = await priceService.UpdatePriceAsync(id, request, cancellationToken);
        return price is null ? NotFound() : Ok(price);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePrice(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await priceService.DeletePriceAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
