using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Repartitions;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Repartitions;

public sealed class RepartitionService(IInvestmentRepository repository) : IRepartitionService
{
    public async Task<IReadOnlyList<RepartitionDto>> GetAssetRepartitionsAsync(CancellationToken cancellationToken = default)
    {
        var repartitions = await repository.GetAssetRepartitionsAsync(cancellationToken);
        return repartitions
            .OrderBy(repartition => repartition.PortfolioId)
            .ThenBy(repartition => repartition.AssetId)
            .Select(ToDto)
            .ToList();
    }

    public async Task<RepartitionDto> CreateAssetRepartitionAsync(CreateRepartitionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateWeightBand(request.TargetWeight, request.MinWeight, request.MaxWeight);
        await ValidateTargetWeightAsync(request.PortfolioId, request.TargetWeight, null, cancellationToken);

        var repartition = new Repartition
        {
            Kind = RepartitionKind.PortfolioAsset,
            PortfolioId = request.PortfolioId,
            AssetId = request.AssetId,
            TargetWeight = request.TargetWeight,
            MinWeight = request.MinWeight,
            MaxWeight = request.MaxWeight,
            Status = request.Status
        };

        await repository.AddRepartitionAsync(repartition, cancellationToken);
        return ToDto(repartition);
    }

    public async Task<RepartitionDto?> UpdateAssetRepartitionAsync(Guid id, UpdateRepartitionRequest request, CancellationToken cancellationToken = default)
    {
        var repartition = await repository.GetAssetRepartitionByIdAsync(id, cancellationToken);
        if (repartition is null)
        {
            return null;
        }

        ValidateWeightBand(request.TargetWeight, request.MinWeight, request.MaxWeight);
        await ValidateTargetWeightAsync(repartition.PortfolioId, request.TargetWeight, repartition.Id, cancellationToken);

        repartition.TargetWeight = request.TargetWeight;
        repartition.MinWeight = request.MinWeight;
        repartition.MaxWeight = request.MaxWeight;
        repartition.Status = request.Status;

        await repository.UpdateRepartitionAsync(repartition, cancellationToken);
        return ToDto(repartition);
    }

    public async Task<bool> DeleteAssetRepartitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var repartition = await repository.GetAssetRepartitionByIdAsync(id, cancellationToken);
        if (repartition is null)
        {
            return false;
        }

        await repository.DeleteRepartitionAsync(repartition, cancellationToken);
        return true;
    }

    private async Task ValidateTargetWeightAsync(Guid portfolioId, decimal targetWeight, Guid? excludedRepartitionId, CancellationToken cancellationToken)
    {
        if (targetWeight is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(targetWeight), "Une cle de repartition doit rester comprise entre 0 et 100 %.");
        }

        var existingWeight = (await repository.GetAssetRepartitionsAsync(cancellationToken))
            .Where(repartition => repartition.PortfolioId == portfolioId && repartition.Id != excludedRepartitionId)
            .Sum(repartition => repartition.TargetWeight);

        if (existingWeight + targetWeight > 1.000001m)
        {
            throw new ArgumentException("La somme des cles de repartition d'un portefeuille ne peut pas depasser 100 %.", nameof(targetWeight));
        }
    }

    private static void ValidateWeightBand(decimal targetWeight, decimal? minWeight, decimal? maxWeight)
    {
        if (targetWeight is < 0m or > 1m
            || minWeight is < 0m or > 1m
            || maxWeight is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(targetWeight), "Les cles de repartition doivent rester comprises entre 0 et 100 %.");
        }

        if (minWeight > targetWeight || maxWeight < targetWeight || minWeight > maxWeight)
        {
            throw new ArgumentException("Les bornes de repartition doivent encadrer la cle cible.");
        }
    }

    private static RepartitionDto ToDto(Repartition repartition) => new(
        repartition.Id,
        repartition.PortfolioId,
        repartition.AssetId!.Value,
        repartition.TargetWeight,
        repartition.MinWeight,
        repartition.MaxWeight,
        repartition.Status!.Value);
}
