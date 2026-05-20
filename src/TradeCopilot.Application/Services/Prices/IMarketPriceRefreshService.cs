using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Prices;

public interface IMarketPriceRefreshService
{
    Task<IReadOnlyList<AssetPrice>> RefreshCurrentPricesAsync(
        IReadOnlyList<Asset> assets,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<AssetPrice> existingPrices,
        CancellationToken cancellationToken = default);
}
