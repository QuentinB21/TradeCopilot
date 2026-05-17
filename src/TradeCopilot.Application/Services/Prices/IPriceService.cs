using TradeCopilot.Application.Contracts.Prices;

namespace TradeCopilot.Application.Services.Prices;

public interface IPriceService
{
    Task<IReadOnlyList<AssetPriceDto>> GetPricesAsync(CancellationToken cancellationToken = default);
    Task<AssetPriceDto> CreatePriceAsync(CreateAssetPriceRequest request, CancellationToken cancellationToken = default);
}
