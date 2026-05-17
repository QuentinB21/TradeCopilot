using Microsoft.AspNetCore.Mvc;
using TradeCopilot.Application.Contracts.Assets;
using TradeCopilot.Application.Services.Assets;

namespace TradeCopilot.Api.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetsController(IAssetService assetService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetAssets(CancellationToken cancellationToken)
    {
        var assets = await assetService.GetAssetsAsync(cancellationToken);
        return Ok(assets);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetDto>> GetAsset(Guid id, CancellationToken cancellationToken)
    {
        var asset = await assetService.GetAssetAsync(id, cancellationToken);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpPost]
    public async Task<ActionResult<AssetDto>> CreateAsset(CreateAssetRequest request, CancellationToken cancellationToken)
    {
        var asset = await assetService.CreateAssetAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, asset);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssetDto>> UpdateAsset(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken)
    {
        var asset = await assetService.UpdateAssetAsync(id, request, cancellationToken);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsset(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await assetService.DeleteAssetAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
