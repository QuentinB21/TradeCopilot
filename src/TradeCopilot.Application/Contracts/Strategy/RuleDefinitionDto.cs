namespace TradeCopilot.Application.Contracts.Strategy;

public sealed record RuleDefinitionDto(
    int Version,
    RuleTargetDto Target,
    RuleConditionDto Condition,
    RuleEffectDto Effect);

public sealed record RuleTargetDto(
    RuleTargetType Type,
    RuleTargetMode Mode,
    Guid? PortfolioId,
    Guid? AssetId);

public sealed record RuleConditionDto(
    RuleConditionMetric Metric,
    RuleComparisonOperator Operator,
    decimal? Value,
    decimal? UpperValue,
    RuleValueUnit Unit,
    RulePeriodDto? Period);

public sealed record RulePeriodDto(
    int Amount,
    RuleTimeUnit Unit);

public sealed record RuleEffectDto(
    RuleEffectType Type,
    RuleEffectStrength Strength,
    RuleSeverity Severity,
    string Message);

public enum RuleTargetType
{
    Asset,
    Portfolio,
    Position
}

public enum RuleTargetMode
{
    All,
    Specific,
    PortfolioAssets
}

public enum RuleConditionMetric
{
    Always,
    PriceChangePercent,
    AllocationDrift,
    UnrealizedGainPercent
}

public enum RuleComparisonOperator
{
    LessThanOrEqual,
    GreaterThanOrEqual,
    Equal,
    BetweenInclusive
}

public enum RuleValueUnit
{
    None,
    Percent,
    PercentPoint
}

public enum RuleTimeUnit
{
    Day,
    Week,
    Month,
    Year
}

public enum RuleEffectType
{
    AlertOnly,
    BlockBuy,
    ReduceBuy,
    PrioritizeBuy,
    RequireReview
}

public enum RuleEffectStrength
{
    Soft,
    Hard
}

public enum RuleSeverity
{
    Info,
    Warning,
    Critical
}
