namespace TradeCopilot.Application.Contracts.Strategy;

public sealed record StrategyRulesExportDto(
    string Format,
    int Version,
    DateTimeOffset ExportedAt,
    IReadOnlyList<PortableStrategyRuleDto> Rules);

public sealed record PortableStrategyRuleDto(
    string Name,
    string Description,
    string? TriggerCondition,
    string RecommendedAction,
    RuleDefinitionDto? Definition,
    int Priority,
    bool IsActive,
    PortablePortfolioReferenceDto? Portfolio,
    PortableAssetReferenceDto? Asset);

public sealed record PortablePortfolioReferenceDto(
    string Name,
    PortfolioTypeReference Type,
    string Broker,
    string BaseCurrency);

public sealed record PortableAssetReferenceDto(
    string Name,
    string Symbol,
    string? Isin,
    string? MarketSymbol,
    string Currency);

public sealed record StrategyRuleImportResultDto(
    int RowsRead,
    int ImportedRules,
    int SkippedRules,
    IReadOnlyList<StrategyRuleImportWarningDto> Warnings);

public sealed record StrategyRuleImportWarningDto(
    int RowNumber,
    string RuleName,
    string Code,
    string Message);

public enum PortfolioTypeReference
{
    Pea,
    SecuritiesAccount,
    Crypto,
    Other
}
