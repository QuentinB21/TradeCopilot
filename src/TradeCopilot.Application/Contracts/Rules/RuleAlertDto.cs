using TradeCopilot.Application.Contracts.Strategy;

namespace TradeCopilot.Application.Contracts.Rules;

public sealed record RuleAlertDto(
    Guid RuleId,
    string RuleName,
    RuleSeverity Severity,
    Guid? PortfolioId,
    string? PortfolioName,
    Guid? AssetId,
    string? AssetName,
    string Message,
    string Explanation,
    decimal? MeasuredValue,
    decimal? ThresholdValue);

public sealed record RuleImpactDto(
    Guid RuleId,
    string RuleName,
    RuleEffectType EffectType,
    RuleEffectStrength Strength,
    RuleSeverity Severity,
    string Message,
    string Explanation);
