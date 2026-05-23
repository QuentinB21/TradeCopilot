using TradeCopilot.Application.Contracts.Positions;
using TradeCopilot.Application.Contracts.Rules;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Application.Services.Strategy;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Rules;

public sealed class RuleEvaluationService
{
    public RuleEvaluationSnapshot Evaluate(
        IReadOnlyList<StrategyRule> rules,
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<PositionDto> positions,
        IReadOnlyList<AssetPrice> prices)
    {
        var portfolioById = portfolios.ToDictionary(portfolio => portfolio.Id);
        var assetById = assets.ToDictionary(asset => asset.Id);
        var alerts = new List<RuleAlertDto>();
        var impacts = new List<AllocationRuleImpact>();

        foreach (var rule in rules.Where(rule => rule.IsActive).OrderBy(rule => rule.Priority).ThenBy(rule => rule.Name))
        {
            var definition = StrategyRuleService.DeserializeDefinition(rule.DefinitionJson);
            if (definition is null)
            {
                continue;
            }

            foreach (var target in ResolveTargets(definition.Target, positions, portfolioById, assetById))
            {
                var evaluation = EvaluateCondition(definition.Condition, target, prices);
                if (!evaluation.IsTriggered)
                {
                    continue;
                }

                var alert = new RuleAlertDto(
                    rule.Id,
                    rule.Name,
                    definition.Effect.Severity,
                    target.PortfolioId,
                    target.PortfolioName,
                    target.AssetId,
                    target.AssetName,
                    definition.Effect.Message,
                    evaluation.Explanation,
                    evaluation.MeasuredValue,
                    definition.Condition.Value);

                alerts.Add(alert);

                if (definition.Effect.Type != RuleEffectType.AlertOnly && target.AssetId is not null)
                {
                    impacts.Add(new AllocationRuleImpact(
                        target.PortfolioId,
                        target.AssetId.Value,
                        new RuleImpactDto(
                            rule.Id,
                            rule.Name,
                            definition.Effect.Type,
                            definition.Effect.Strength,
                            definition.Effect.Severity,
                            definition.Effect.Message,
                            evaluation.Explanation)));
                }
            }
        }

        return new RuleEvaluationSnapshot(alerts, impacts);
    }

    private static IEnumerable<RuleTargetContext> ResolveTargets(
        RuleTargetDto target,
        IReadOnlyList<PositionDto> positions,
        IReadOnlyDictionary<Guid, Portfolio> portfolioById,
        IReadOnlyDictionary<Guid, Asset> assetById)
    {
        if (target.Type is RuleTargetType.Asset or RuleTargetType.Position)
        {
            return positions
                .Where(position => target.Mode switch
                {
                    RuleTargetMode.All => true,
                    RuleTargetMode.Specific => position.AssetId == target.AssetId,
                    RuleTargetMode.PortfolioAssets => position.PortfolioId == target.PortfolioId,
                    _ => false
                })
                .Select(position => new RuleTargetContext(
                    position.PortfolioId,
                    position.PortfolioName,
                    position.AssetId,
                    position.AssetName,
                    position));
        }

        return portfolioById.Values
            .Where(portfolio => target.Mode == RuleTargetMode.All
                || target.Mode == RuleTargetMode.Specific && portfolio.Id == target.PortfolioId)
            .Select(portfolio => new RuleTargetContext(
                portfolio.Id,
                portfolio.Name,
                null,
                null,
                null));
    }

    private static RuleConditionEvaluation EvaluateCondition(
        RuleConditionDto condition,
        RuleTargetContext target,
        IReadOnlyList<AssetPrice> prices)
    {
        if (condition.Metric == RuleConditionMetric.Always)
        {
            return new RuleConditionEvaluation(true, null, "Regle active sans condition de marche.");
        }

        if (target.Position is null)
        {
            return new RuleConditionEvaluation(false, null, "La cible n'a pas de position evaluable.");
        }

        var measuredValue = condition.Metric switch
        {
            RuleConditionMetric.AllocationDrift => target.Position.AllocationDrift,
            RuleConditionMetric.UnrealizedGainPercent => target.Position.UnrealizedGainPercent,
            RuleConditionMetric.PriceChangePercent => GetPriceChange(target.Position.AssetId, condition.Period, prices),
            _ => null
        };

        if (measuredValue is null || condition.Value is null)
        {
            return new RuleConditionEvaluation(false, measuredValue, "Donnees insuffisantes pour evaluer la regle.");
        }

        var triggered = condition.Operator switch
        {
            RuleComparisonOperator.LessThanOrEqual => measuredValue.Value <= condition.Value.Value,
            RuleComparisonOperator.GreaterThanOrEqual => measuredValue.Value >= condition.Value.Value,
            RuleComparisonOperator.Equal => measuredValue.Value == condition.Value.Value,
            _ => false
        };

        return new RuleConditionEvaluation(
            triggered,
            measuredValue,
            $"{FormatMetric(condition.Metric)} mesure a {FormatPercent(measuredValue.Value)} pour un seuil de {FormatPercent(condition.Value.Value)}.");
    }

    private static decimal? GetPriceChange(Guid assetId, RulePeriodDto? period, IReadOnlyList<AssetPrice> prices)
    {
        if (period is null)
        {
            return null;
        }

        var assetPrices = prices
            .Where(price => price.AssetId == assetId)
            .OrderBy(price => price.Date)
            .ToList();
        if (assetPrices.Count < 2)
        {
            return null;
        }

        var latest = assetPrices[^1];
        var referenceDate = latest.Date.AddDays(-PeriodInDays(period));
        var reference = assetPrices.LastOrDefault(price => price.Date <= referenceDate) ?? assetPrices[0];
        return reference.Close > 0m ? decimal.Round((latest.Close - reference.Close) / reference.Close, 4, MidpointRounding.AwayFromZero) : null;
    }

    private static int PeriodInDays(RulePeriodDto period) =>
        period.Unit switch
        {
            RuleTimeUnit.Day => period.Amount,
            RuleTimeUnit.Week => period.Amount * 7,
            RuleTimeUnit.Month => period.Amount * 30,
            RuleTimeUnit.Year => period.Amount * 365,
            _ => period.Amount
        };

    private static string FormatMetric(RuleConditionMetric metric) =>
        metric switch
        {
            RuleConditionMetric.PriceChangePercent => "Variation du cours",
            RuleConditionMetric.AllocationDrift => "Ecart a la cible",
            RuleConditionMetric.UnrealizedGainPercent => "Gain latent",
            _ => "Condition"
        };

    private static string FormatPercent(decimal value) => $"{decimal.Round(value * 100m, 2, MidpointRounding.AwayFromZero)} %";

    private sealed record RuleConditionEvaluation(bool IsTriggered, decimal? MeasuredValue, string Explanation);

    private sealed record RuleTargetContext(
        Guid? PortfolioId,
        string? PortfolioName,
        Guid? AssetId,
        string? AssetName,
        PositionDto? Position);
}

public sealed record RuleEvaluationSnapshot(
    IReadOnlyList<RuleAlertDto> Alerts,
    IReadOnlyList<AllocationRuleImpact> AllocationImpacts);

public sealed record AllocationRuleImpact(
    Guid? PortfolioId,
    Guid AssetId,
    RuleImpactDto Impact);
