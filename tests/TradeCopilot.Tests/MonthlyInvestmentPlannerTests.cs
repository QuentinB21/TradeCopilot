using TradeCopilot.Application;
using TradeCopilot.Application.Contracts.Rules;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Application.Services.Rules;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class MonthlyInvestmentPlannerTests
{
    [Fact]
    public void Splits_monthly_investment_between_pea_and_trade_republic()
    {
        var seed = TestInvestmentSeedData.Create();
        var positions = new PositionCalculator().Calculate(
            seed.Portfolios,
            seed.Assets,
            seed.Transactions,
            seed.Prices,
            seed.Repartitions);

        var plan = new MonthlyInvestmentPlanner().BuildPlan(
            400m,
            seed.Portfolios,
            seed.Assets,
            positions,
            seed.Repartitions);

        Assert.Equal(400m, plan.Amount);
        Assert.Contains(plan.Envelopes, envelope => envelope.PortfolioName == "PEA BoursoBank" && envelope.Amount == 320m);
        Assert.Contains(plan.Envelopes, envelope => envelope.PortfolioName == "Trade Republic" && envelope.Amount == 80m);
        Assert.DoesNotContain(plan.Envelopes.SelectMany(envelope => envelope.Lines), line => line.Symbol == "PLTR");
    }

    [Fact]
    public void Applies_rule_impacts_to_monthly_recommendations()
    {
        var seed = TestInvestmentSeedData.Create();
        var positions = new PositionCalculator().Calculate(
            seed.Portfolios,
            seed.Assets,
            seed.Transactions,
            seed.Prices,
            seed.Repartitions);
        var microsoft = seed.Assets.Single(asset => asset.Symbol == "MSFT");
        var tradeRepublic = seed.Portfolios.Single(portfolio => portfolio.Name == "Trade Republic");

        var plan = new MonthlyInvestmentPlanner().BuildPlan(
            400m,
            seed.Portfolios,
            seed.Assets,
            positions,
            seed.Repartitions,
            [
                new AllocationRuleImpact(
                    tradeRepublic.Id,
                    microsoft.Id,
                    new RuleImpactDto(
                        Guid.NewGuid(),
                        "Pause Microsoft",
                        RuleEffectType.BlockBuy,
                        RuleEffectStrength.Hard,
                        RuleSeverity.Warning,
                        "Ne pas renforcer Microsoft ce mois-ci.",
                        "Regle de test declenchee."))
            ]);

        var microsoftLine = plan.Envelopes
            .Single(envelope => envelope.PortfolioName == "Trade Republic")
            .Lines
            .Single(line => line.Symbol == "MSFT");

        Assert.Equal(0m, microsoftLine.Amount);
        Assert.Contains(microsoftLine.RuleImpacts, impact => impact.EffectType == RuleEffectType.BlockBuy);
    }
}
