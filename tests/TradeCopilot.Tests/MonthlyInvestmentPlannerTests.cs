using TradeCopilot.Application;
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
            seed.AllocationRules);

        var plan = new MonthlyInvestmentPlanner().BuildPlan(
            400m,
            seed.Portfolios,
            seed.Assets,
            positions,
            seed.AllocationRules);

        Assert.Equal(400m, plan.Amount);
        Assert.Contains(plan.Envelopes, envelope => envelope.PortfolioName == "PEA BoursoBank" && envelope.Amount == 320m);
        Assert.Contains(plan.Envelopes, envelope => envelope.PortfolioName == "Trade Republic" && envelope.Amount == 80m);
        Assert.DoesNotContain(plan.Envelopes.SelectMany(envelope => envelope.Lines), line => line.Symbol == "PLTR");
    }
}
