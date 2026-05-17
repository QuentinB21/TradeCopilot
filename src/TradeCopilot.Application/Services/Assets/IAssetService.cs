using TradeCopilot.Application.Contracts.Assets;

namespace TradeCopilot.Application.Services.Assets;

public interface IAssetService
{
    Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken cancellationToken = default);
    Task<AssetDto?> GetAssetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetDto> CreateAssetAsync(CreateAssetRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto?> UpdateAssetAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAssetAsync(Guid id, CancellationToken cancellationToken = default);
}
