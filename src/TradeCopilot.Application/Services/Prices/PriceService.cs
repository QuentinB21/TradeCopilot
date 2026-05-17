using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Prices;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Prices;

public sealed class PriceService(IInvestmentRepository repository) : IPriceService
{
    public async Task<IReadOnlyList<AssetPriceDto>> GetPricesAsync(CancellationToken cancellationToken = default)
    {
        var prices = await repository.GetPricesAsync(cancellationToken);
        return prices
            .OrderByDescending(price => price.Date)
            .Select(ToDto)
            .ToList();
    }

    public async Task<AssetPriceDto> CreatePriceAsync(CreateAssetPriceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Source);

        var price = new AssetPrice
        {
            AssetId = request.AssetId,
            Date = request.Date,
            Open = request.Open,
            High = request.High,
            Low = request.Low,
            Close = request.Close,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Source = request.Source.Trim()
        };

        await repository.AddPriceAsync(price, cancellationToken);
        return ToDto(price);
    }

    private static AssetPriceDto ToDto(AssetPrice price) => new(
        price.Id,
        price.AssetId,
        price.Date,
        price.Open,
        price.High,
        price.Low,
        price.Close,
        price.Currency,
        price.Source);
}
