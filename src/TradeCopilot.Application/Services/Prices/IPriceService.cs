using TradeCopilot.Application.Contracts.Prices;

namespace TradeCopilot.Application.Services.Prices;

public interface IPriceService
{
    Task<IReadOnlyList<AssetPriceDto>> GetPricesAsync(CancellationToken cancellationToken = default);
    Task<AssetPriceDto?> GetPriceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetPriceDto> CreatePriceAsync(CreateAssetPriceRequest request, CancellationToken cancellationToken = default);
    Task<AssetPriceDto?> UpdatePriceAsync(Guid id, UpdateAssetPriceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePriceAsync(Guid id, CancellationToken cancellationToken = default);
}
