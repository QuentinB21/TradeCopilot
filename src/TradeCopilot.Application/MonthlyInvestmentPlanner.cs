using TradeCopilot.Application.Contracts.InvestmentPlans;
using TradeCopilot.Application.Contracts.Positions;
using TradeCopilot.Application.Contracts.Rules;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Application.Services.Rules;
using TradeCopilot.Domain;

namespace TradeCopilot.Application;

public sealed class MonthlyInvestmentPlanner
{
    public MonthlyInvestmentPlanDto BuildPlan(
        decimal amount,
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<PositionDto> positions,
        IReadOnlyList<Repartition> repartitions,
        IReadOnlyList<AllocationRuleImpact>? allocationImpacts = null)
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
                repartitions.Where(repartition => repartition.PortfolioId == portfolio.Id).ToList(),
                allocationImpacts ?? []));
        }

        return new MonthlyInvestmentPlanDto(decimal.Round(amount, 2), envelopes, notes);
    }

    private static InvestmentEnvelopeRecommendationDto BuildEnvelope(
        Portfolio portfolio,
        decimal amount,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<PositionDto> positions,
        IReadOnlyList<Repartition> repartitions,
        IReadOnlyList<AllocationRuleImpact> allocationImpacts)
    {
        var assetById = assets.ToDictionary(asset => asset.Id);
        var positionByAssetId = positions.ToDictionary(position => position.AssetId);
        var activeRepartitions = repartitions
            .Where(repartition => repartition.Status == RepartitionStatus.Active)
            .Where(repartition => assetById.TryGetValue(repartition.AssetId!.Value, out var asset) &&
                           asset.StrategicStatus is StrategicStatus.Core or StrategicStatus.Conviction)
            .ToList();

        var weightedLines = activeRepartitions.Select(repartition =>
        {
            var asset = assetById[repartition.AssetId!.Value];
            var impacts = allocationImpacts
                .Where(impact => impact.AssetId == asset.Id && (impact.PortfolioId is null || impact.PortfolioId == portfolio.Id))
                .Select(impact => impact.Impact)
                .ToList();

            return new WeightedRepartition(repartition, asset, impacts, ApplyRuleWeight(repartition.TargetWeight, impacts));
        }).ToList();

        var totalTarget = weightedLines.Sum(line => line.AdjustedTargetWeight);
        var lines = weightedLines.Select(line =>
        {
            var asset = line.Asset;
            positionByAssetId.TryGetValue(asset.Id, out var position);
            var normalizedWeight = totalTarget > 0m ? line.AdjustedTargetWeight / totalTarget : 0m;
            var lineAmount = decimal.Round(amount * normalizedWeight, 2, MidpointRounding.AwayFromZero);
            var currentWeight = position?.Weight;

            var rationale = line.Impacts.Count > 0
                ? string.Join(" ", line.Impacts.Select(impact => impact.Message).Distinct())
                : currentWeight.HasValue && currentWeight.Value > line.Repartition.TargetWeight
                ? "Sous-allocation du versement car la ligne est deja au-dessus de sa cible."
                : "Renforcement conforme a la cible strategique.";

            return new InvestmentLineRecommendationDto(
                asset.Id,
                asset.Symbol,
                asset.Name,
                lineAmount,
                line.Repartition.TargetWeight,
                currentWeight,
                rationale,
                line.Impacts);
        }).ToList();

        return new InvestmentEnvelopeRecommendationDto(
            portfolio.Id,
            portfolio.Name,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            lines);
    }

    private static decimal ApplyRuleWeight(decimal targetWeight, IReadOnlyList<RuleImpactDto> impacts)
    {
        if (impacts.Any(impact => impact.EffectType == RuleEffectType.BlockBuy))
        {
            return 0m;
        }

        var adjustedWeight = targetWeight;
        foreach (var impact in impacts)
        {
            adjustedWeight *= impact.EffectType switch
            {
                RuleEffectType.ReduceBuy => impact.Strength == RuleEffectStrength.Hard ? 0.35m : 0.65m,
                RuleEffectType.PrioritizeBuy => impact.Strength == RuleEffectStrength.Hard ? 1.6m : 1.3m,
                RuleEffectType.RequireReview => 1m,
                _ => 1m
            };
        }

        return adjustedWeight;
    }

    private sealed record WeightedRepartition(
        Repartition Repartition,
        Asset Asset,
        IReadOnlyList<RuleImpactDto> Impacts,
        decimal AdjustedTargetWeight);
}
