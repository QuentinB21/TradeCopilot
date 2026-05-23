using System.Text.Json;
using System.Text.Json.Serialization;
using TradeCopilot.Application.Contracts.Positions;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Application.Services.Rules;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class RuleEvaluationServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Triggers_price_drop_between_two_thresholds()
    {
        var portfolioId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var rule = new StrategyRule
        {
            Id = Guid.NewGuid(),
            Name = "Baisse passagere",
            Description = "Informer quand la baisse reste moderee.",
            RecommendedAction = "Ne pas paniquer.",
            IsActive = true,
            DefinitionJson = JsonSerializer.Serialize(new RuleDefinitionDto(
                1,
                new RuleTargetDto(RuleTargetType.Asset, RuleTargetMode.All, null, null),
                new RuleConditionDto(
                    RuleConditionMetric.PriceChangePercent,
                    RuleComparisonOperator.BetweenInclusive,
                    -0.05m,
                    0m,
                    RuleValueUnit.Percent,
                    new RulePeriodDto(1, RuleTimeUnit.Month)),
                new RuleEffectDto(RuleEffectType.AlertOnly, RuleEffectStrength.Soft, RuleSeverity.Info, "Ne pas paniquer.")), JsonOptions)
        };

        var snapshot = new RuleEvaluationService().Evaluate(
            [rule],
            [new Portfolio { Id = portfolioId, Name = "PEA", Broker = "Broker", BaseCurrency = "EUR" }],
            [new Asset { Id = assetId, Name = "Microsoft", Symbol = "MSFT", Type = AssetType.Stock, Currency = "EUR", StrategicStatus = StrategicStatus.Core }],
            [Position(portfolioId, assetId)],
            [
                Price(assetId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-31), 100m),
                Price(assetId, DateOnly.FromDateTime(DateTime.UtcNow), 97m)
            ]);

        var alert = Assert.Single(snapshot.Alerts);
        Assert.Equal(assetId, alert.AssetId);
        Assert.Equal(-0.03m, alert.MeasuredValue);
    }

    private static PositionDto Position(Guid portfolioId, Guid assetId) => new(
        portfolioId,
        "PEA",
        assetId,
        "Microsoft",
        "MSFT",
        StrategicStatus.Core,
        1m,
        100m,
        100m,
        97m,
        true,
        97m,
        -3m,
        -0.03m,
        0m,
        1m,
        1m,
        0m);

    private static AssetPrice Price(Guid assetId, DateOnly date, decimal close) => new()
    {
        AssetId = assetId,
        Date = date,
        Close = close,
        Currency = "EUR",
        Source = "test"
    };
}
