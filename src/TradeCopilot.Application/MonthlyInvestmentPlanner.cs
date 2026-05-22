using TradeCopilot.Application.Contracts.InvestmentPlans;
using TradeCopilot.Application.Contracts.Positions;
using TradeCopilot.Domain;

namespace TradeCopilot.Application;

public sealed class MonthlyInvestmentPlanner
{
    public MonthlyInvestmentPlanDto BuildPlan(
        decimal amount,
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<PositionDto> positions,
        IReadOnlyList<Repartition> repartitions)
    {
        var notes = new List<string>
        {
            "Aucune operation reelle n'est executee automatiquement. Validation humaine obligatoire.",
            "Les lignes gelees, en observation stricte ou en sortie planifiee sont exclues des renforcements."
        };

        var configuredPortfolios = portfolios
            .Where(portfolio => portfolio.TargetWeight > 0m)
            .OrderBy(portfolio => portfolio.Name)
            .ToList();
        var totalPortfolioTarget = configuredPortfolios.Sum(portfolio => portfolio.TargetWeight);
        var envelopes = new List<InvestmentEnvelopeRecommendationDto>();

        foreach (var portfolio in configuredPortfolios)
        {
            var normalizedPortfolioWeight = totalPortfolioTarget > 0m ? portfolio.TargetWeight / totalPortfolioTarget : 0m;
            envelopes.Add(BuildEnvelope(
                portfolio,
                amount * normalizedPortfolioWeight,
                assets,
                positions.Where(position => position.PortfolioId == portfolio.Id).ToList(),
                repartitions.Where(repartition => repartition.PortfolioId == portfolio.Id).ToList()));
        }

        return new MonthlyInvestmentPlanDto(decimal.Round(amount, 2), envelopes, notes);
    }

    private static InvestmentEnvelopeRecommendationDto BuildEnvelope(
        Portfolio portfolio,
        decimal amount,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<PositionDto> positions,
        IReadOnlyList<Repartition> repartitions)
    {
        var assetById = assets.ToDictionary(asset => asset.Id);
        var positionByAssetId = positions.ToDictionary(position => position.AssetId);
        var activeRepartitions = repartitions
            .Where(repartition => repartition.Status == RepartitionStatus.Active)
            .Where(repartition => assetById.TryGetValue(repartition.AssetId!.Value, out var asset) &&
                           asset.StrategicStatus is StrategicStatus.Core or StrategicStatus.Conviction)
            .ToList();

        var totalTarget = activeRepartitions.Sum(repartition => repartition.TargetWeight);
        var lines = activeRepartitions.Select(repartition =>
        {
            var asset = assetById[repartition.AssetId!.Value];
            positionByAssetId.TryGetValue(asset.Id, out var position);
            var normalizedWeight = totalTarget > 0m ? repartition.TargetWeight / totalTarget : 0m;
            var lineAmount = decimal.Round(amount * normalizedWeight, 2, MidpointRounding.AwayFromZero);
            var currentWeight = position?.Weight;

            var rationale = currentWeight.HasValue && currentWeight.Value > repartition.TargetWeight
                ? "Sous-allocation du versement car la ligne est deja au-dessus de sa cible."
                : "Renforcement conforme a la cible strategique.";

            return new InvestmentLineRecommendationDto(
                asset.Id,
                asset.Symbol,
                asset.Name,
                lineAmount,
                repartition.TargetWeight,
                currentWeight,
                rationale);
        }).ToList();

        return new InvestmentEnvelopeRecommendationDto(
            portfolio.Id,
            portfolio.Name,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            lines);
    }
}
