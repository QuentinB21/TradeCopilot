using TradeCopilot.Application.Contracts.Strategy;

namespace TradeCopilot.Application.Services.Strategy;

public sealed class StrategyService : IStrategyService
{
    public StrategyDto GetStrategy() => new(
        [
            new GlobalAllocationTargetDto("PEA BoursoBank", 0.80m),
            new GlobalAllocationTargetDto("Trade Republic", 0.20m)
        ],
        [
            "Acheter tous les mois entre le 10 et le 15.",
            "Viser 5 000 EUR sur WPEA avant diversification.",
            "Baisse > 10% : renfort prioritaire si cash disponible.",
            "Baisse > 20% : opportunite majeure, validation humaine requise."
        ],
        [
            "Renforcer uniquement les lignes actives de conviction.",
            "Palantir : observation, aucun renfort automatique.",
            "Spotify et Strategy/MicroStrategy : sortie planifiee.",
            "Oracle : gelee jusqu'aux resultats financiers."
        ]);
}
