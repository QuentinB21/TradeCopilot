using TradeCopilot.Application.Contracts.InvestmentPlans;
using TradeCopilot.Application.Contracts.Positions;
using TradeCopilot.Domain;

namespace TradeCopilot.Application;

public sealed class MonthlyInvestmentPlanner
{
    private const decimal PeaTargetShare = 0.80m;
    private const decimal TradeRepublicTargetShare = 0.20m;

    public MonthlyInvestmentPlanDto BuildPlan(
        decimal amount,
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<PositionDto> positions,
        IReadOnlyList<AllocationRule> allocationRules)
    {
        var notes = new List<string>
        {
            "Aucune operation reelle n'est executee automatiquement. Validation humaine obligatoire.",
            "Les lignes gelees, en observation stricte ou en sortie planifiee sont exclues des renforcements."
        };

        var pea = portfolios.FirstOrDefault(portfolio => portfolio.Type == PortfolioType.Pea);
        var tradeRepublic = portfolios.FirstOrDefault(portfolio =>
            portfolio.Type == PortfolioType.SecuritiesAccount &&
            portfolio.Broker.Contains("Trade Republic", StringComparison.OrdinalIgnoreCase));

        var envelopes = new List<InvestmentEnvelopeRecommendationDto>();

        if (pea is not null)
        {
            envelopes.Add(BuildEnvelope(
                pea,
                amount * PeaTargetShare,
                assets,
                positions.Where(position => position.PortfolioId == pea.Id).ToList(),
                allocationRules.Where(rule => rule.PortfolioId == pea.Id).ToList()));
        }

        if (tradeRepublic is not null)
        {
            envelopes.Add(BuildEnvelope(
                tradeRepublic,
                amount * TradeRepublicTargetShare,
                assets,
                positions.Where(position => position.PortfolioId == tradeRepublic.Id).ToList(),
                allocationRules.Where(rule => rule.PortfolioId == tradeRepublic.Id).ToList()));
        }

        return new MonthlyInvestmentPlanDto(decimal.Round(amount, 2), envelopes, notes);
    }

    private static InvestmentEnvelopeRecommendationDto BuildEnvelope(
        Portfolio portfolio,
        decimal amount,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<PositionDto> positions,
        IReadOnlyList<AllocationRule> rules)
    {
        var assetById = assets.ToDictionary(asset => asset.Id);
        var positionByAssetId = positions.ToDictionary(position => position.AssetId);
        var activeRules = rules
            .Where(rule => rule.Status == AllocationRuleStatus.Active)
            .Where(rule => assetById.TryGetValue(rule.AssetId, out var asset) &&
                           asset.StrategicStatus is StrategicStatus.Core or StrategicStatus.Conviction)
            .ToList();

        var totalTarget = activeRules.Sum(rule => rule.TargetWeight);
        var lines = activeRules.Select(rule =>
        {
            var asset = assetById[rule.AssetId];
            positionByAssetId.TryGetValue(asset.Id, out var position);
            var normalizedWeight = totalTarget > 0m ? rule.TargetWeight / totalTarget : 0m;
            var lineAmount = decimal.Round(amount * normalizedWeight, 2, MidpointRounding.AwayFromZero);
            var currentWeight = position?.Weight;

            var rationale = currentWeight.HasValue && currentWeight.Value > rule.TargetWeight
                ? "Sous-allocation du versement car la ligne est deja au-dessus de sa cible."
                : "Renforcement conforme a la cible strategique.";

            return new InvestmentLineRecommendationDto(
                asset.Id,
                asset.Symbol,
                asset.Name,
                lineAmount,
                rule.TargetWeight,
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
