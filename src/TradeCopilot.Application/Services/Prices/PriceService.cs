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

    public async Task<AssetPriceDto?> GetPriceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var price = await repository.GetPriceByIdAsync(id, cancellationToken);
        return price is null ? null : ToDto(price);
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

    public async Task<AssetPriceDto?> UpdatePriceAsync(Guid id, UpdateAssetPriceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Source);

        var price = await repository.GetPriceByIdAsync(id, cancellationToken);
        if (price is null)
        {
            return null;
        }

        price.AssetId = request.AssetId;
        price.Date = request.Date;
        price.Open = request.Open;
        price.High = request.High;
        price.Low = request.Low;
        price.Close = request.Close;
        price.Currency = request.Currency.Trim().ToUpperInvariant();
        price.Source = request.Source.Trim();

        await repository.UpdatePriceAsync(price, cancellationToken);
        return ToDto(price);
    }

    public async Task<bool> DeletePriceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var price = await repository.GetPriceByIdAsync(id, cancellationToken);
        if (price is null)
        {
            return false;
        }

        await repository.DeletePriceAsync(price, cancellationToken);
        return true;
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
