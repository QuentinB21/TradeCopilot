using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Assets;
using TradeCopilot.Application.Contracts.Common;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Assets;

public sealed class AssetService(IInvestmentRepository repository) : IAssetService
{
    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken cancellationToken = default)
    {
        var assets = await repository.GetAssetsAsync(cancellationToken);
        return assets
            .OrderBy(asset => asset.Name)
            .ThenBy(asset => asset.Symbol)
            .Select(ToDto)
            .ToList();
    }

    public async Task<AssetDto?> GetAssetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetAssetByIdAsync(id, cancellationToken);
        return asset is null ? null : ToDto(asset);
    }

    public async Task<AssetDto> CreateAssetAsync(CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Currency);
        ValidateStrategicStatus(request.StrategicStatus);

        var asset = new Asset
        {
            Name = request.Name.Trim(),
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            Isin = string.IsNullOrWhiteSpace(request.Isin) ? null : request.Isin.Trim().ToUpperInvariant(),
            Type = request.Type,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Sector = request.Sector?.Trim(),
            Country = request.Country?.Trim(),
            PriceProvider = request.PriceProvider?.Trim(),
            MarketSymbol = NormalizeMarketSymbol(request.MarketSymbol),
            StrategicStatus = request.StrategicStatus
        };

        await repository.AddAssetAsync(asset, cancellationToken);
        return ToDto(asset);
    }

    public async Task<AssetDto?> UpdateAssetAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Currency);
        ValidateStrategicStatus(request.StrategicStatus);

        var asset = await repository.GetAssetByIdAsync(id, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        asset.Name = request.Name.Trim();
        asset.Symbol = request.Symbol.Trim().ToUpperInvariant();
        asset.Isin = string.IsNullOrWhiteSpace(request.Isin) ? null : request.Isin.Trim().ToUpperInvariant();
        asset.Type = request.Type;
        asset.Currency = request.Currency.Trim().ToUpperInvariant();
        asset.Sector = request.Sector?.Trim();
        asset.Country = request.Country?.Trim();
        asset.PriceProvider = request.PriceProvider?.Trim();
        asset.MarketSymbol = NormalizeMarketSymbol(request.MarketSymbol);
        asset.StrategicStatus = request.StrategicStatus;

        await repository.UpdateAssetAsync(asset, cancellationToken);
        return ToDto(asset);
    }

    public async Task<AssetDto?> BindMarketInstrumentAsync(Guid id, BindMarketInstrumentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MarketSymbol);

        var asset = await repository.GetAssetByIdAsync(id, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        asset.MarketSymbol = NormalizeMarketSymbol(request.MarketSymbol);
        asset.PriceProvider = string.IsNullOrWhiteSpace(request.PriceProvider) ? asset.PriceProvider : request.PriceProvider.Trim();

        await repository.UpdateAssetAsync(asset, cancellationToken);
        return ToDto(asset);
    }

    public async Task<DeleteEntityResult> DeleteAssetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetAssetByIdAsync(id, cancellationToken);
        if (asset is null)
        {
            return DeleteEntityResult.NotFound;
        }

        if (await repository.AssetHasReferencesAsync(id, cancellationToken))
        {
            return DeleteEntityResult.Conflict;
        }

        await repository.DeleteAssetAsync(asset, cancellationToken);
        return DeleteEntityResult.Deleted;
    }

    private static AssetDto ToDto(Asset asset) => new(
        asset.Id,
        asset.Name,
        asset.Symbol,
        asset.Isin,
        asset.Type,
        asset.Currency,
        asset.Sector,
        asset.PriceProvider,
        asset.MarketSymbol,
        asset.StrategicStatus);

    private static string? NormalizeMarketSymbol(string? marketSymbol) =>
        string.IsNullOrWhiteSpace(marketSymbol) ? null : marketSymbol.Trim().ToUpperInvariant();

    private static void ValidateStrategicStatus(StrategicStatus strategicStatus)
    {
        if (strategicStatus is not (StrategicStatus.Core or StrategicStatus.Conviction or StrategicStatus.Observation or StrategicStatus.PlannedExit))
        {
            throw new ArgumentException("Le role strategique d'un actif doit etre Noyau, Conviction, Observation ou A ceder.", nameof(strategicStatus));
        }
    }
}
