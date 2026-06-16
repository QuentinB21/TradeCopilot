using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradeCopilot.Application.Services.Strategy;

public sealed class StrategyRuleService(IInvestmentRepository repository) : IStrategyRuleService
{
    private const string ExportFormat = "tradecopilot.strategy-rules";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<StrategyRuleDto>> GetStrategyRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await repository.GetStrategyRulesAsync(cancellationToken);
        return rules.OrderBy(rule => rule.Priority).ThenBy(rule => rule.Name).Select(ToDto).ToList();
    }

    public async Task<StrategyRulesExportDto> ExportStrategyRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await repository.GetStrategyRulesAsync(cancellationToken);
        var portfolios = await repository.GetPortfoliosAsync(cancellationToken);
        var assets = await repository.GetAssetsAsync(cancellationToken);
        var portfolioById = portfolios.ToDictionary(portfolio => portfolio.Id);
        var assetById = assets.ToDictionary(asset => asset.Id);

        return new StrategyRulesExportDto(
            ExportFormat,
            1,
            DateTimeOffset.UtcNow,
            rules
                .OrderBy(rule => rule.Priority)
                .ThenBy(rule => rule.Name)
                .Select(rule => ToPortableDto(rule, portfolioById, assetById))
                .ToList());
    }

    public async Task<StrategyRuleImportResultDto> ImportStrategyRulesAsync(StrategyRulesExportDto importFile, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(importFile.Format, ExportFormat, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Le fichier ne correspond pas a un export de regles TradeCopilot.", nameof(importFile));
        }

        if (importFile.Version != 1)
        {
            throw new ArgumentException("La version du fichier de regles n'est pas supportee.", nameof(importFile));
        }

        var portfolios = await repository.GetPortfoliosAsync(cancellationToken);
        var assets = await repository.GetAssetsAsync(cancellationToken);
        var warnings = new List<StrategyRuleImportWarningDto>();
        var importedRules = 0;

        for (var index = 0; index < importFile.Rules.Count; index++)
        {
            var rowNumber = index + 1;
            var importedRule = importFile.Rules[index];

            try
            {
                var resolution = ResolveImportedRule(importedRule, portfolios, assets);
                if (resolution.SkipMessage is not null)
                {
                    warnings.Add(new StrategyRuleImportWarningDto(rowNumber, importedRule.Name, "MissingReference", resolution.SkipMessage));
                    continue;
                }

                var rule = new StrategyRule
                {
                    PortfolioId = resolution.PortfolioId,
                    AssetId = resolution.AssetId,
                    Name = NormalizeRequired(importedRule.Name),
                    Description = NormalizeRequired(importedRule.Description),
                    TriggerCondition = NormalizeOptional(importedRule.TriggerCondition),
                    RecommendedAction = NormalizeRequired(importedRule.RecommendedAction),
                    DefinitionJson = SerializeDefinition(ValidateDefinition(resolution.Definition)),
                    Priority = importedRule.Priority,
                    IsActive = importedRule.IsActive
                };

                await repository.AddStrategyRuleAsync(rule, cancellationToken);
                importedRules++;
            }
            catch (ArgumentException exception)
            {
                warnings.Add(new StrategyRuleImportWarningDto(rowNumber, importedRule.Name, "InvalidRule", exception.Message));
            }
        }

        return new StrategyRuleImportResultDto(
            importFile.Rules.Count,
            importedRules,
            importFile.Rules.Count - importedRules,
            warnings);
    }

    public async Task<StrategyRuleDto> CreateStrategyRuleAsync(CreateStrategyRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = new StrategyRule
        {
            PortfolioId = request.PortfolioId,
            AssetId = request.AssetId,
            Name = NormalizeRequired(request.Name),
            Description = NormalizeRequired(request.Description),
            TriggerCondition = NormalizeOptional(request.TriggerCondition),
            RecommendedAction = NormalizeRequired(request.RecommendedAction),
            DefinitionJson = SerializeDefinition(ValidateDefinition(request.Definition)),
            Priority = request.Priority,
            IsActive = request.IsActive
        };

        await repository.AddStrategyRuleAsync(rule, cancellationToken);
        return ToDto(rule);
    }

    public async Task<StrategyRuleDto?> UpdateStrategyRuleAsync(Guid id, UpdateStrategyRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetStrategyRuleByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        rule.PortfolioId = request.PortfolioId;
        rule.AssetId = request.AssetId;
        rule.Name = NormalizeRequired(request.Name);
        rule.Description = NormalizeRequired(request.Description);
        rule.TriggerCondition = NormalizeOptional(request.TriggerCondition);
        rule.RecommendedAction = NormalizeRequired(request.RecommendedAction);
        rule.DefinitionJson = SerializeDefinition(ValidateDefinition(request.Definition));
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;

        await repository.UpdateStrategyRuleAsync(rule, cancellationToken);
        return ToDto(rule);
    }

    public async Task<bool> DeleteStrategyRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetStrategyRuleByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        await repository.DeleteStrategyRuleAsync(rule, cancellationToken);
        return true;
    }

    private static StrategyRuleDto ToDto(StrategyRule rule) => new(
        rule.Id,
        rule.PortfolioId,
        rule.AssetId,
        rule.Name,
        rule.Description,
        rule.TriggerCondition,
        rule.RecommendedAction,
        DeserializeDefinition(rule.DefinitionJson),
        rule.Priority,
        rule.IsActive);

    private static PortableStrategyRuleDto ToPortableDto(
        StrategyRule rule,
        IReadOnlyDictionary<Guid, Portfolio> portfolioById,
        IReadOnlyDictionary<Guid, Asset> assetById)
    {
        var definition = DeserializeDefinition(rule.DefinitionJson);
        var portfolioId = definition?.Target.PortfolioId ?? rule.PortfolioId;
        var assetId = definition?.Target.AssetId ?? rule.AssetId;
        var portableDefinition = RebindDefinition(definition, null, null);

        return new PortableStrategyRuleDto(
            rule.Name,
            rule.Description,
            rule.TriggerCondition,
            rule.RecommendedAction,
            portableDefinition,
            rule.Priority,
            rule.IsActive,
            portfolioId is not null && portfolioById.TryGetValue(portfolioId.Value, out var portfolio)
                ? ToPortfolioReference(portfolio)
                : null,
            assetId is not null && assetById.TryGetValue(assetId.Value, out var asset)
                ? ToAssetReference(asset)
                : null);
    }

    private static PortablePortfolioReferenceDto ToPortfolioReference(Portfolio portfolio) => new(
        portfolio.Name,
        portfolio.Type switch
        {
            PortfolioType.Pea => PortfolioTypeReference.Pea,
            PortfolioType.SecuritiesAccount => PortfolioTypeReference.SecuritiesAccount,
            PortfolioType.Crypto => PortfolioTypeReference.Crypto,
            _ => PortfolioTypeReference.Other
        },
        portfolio.Broker,
        portfolio.BaseCurrency);

    private static PortableAssetReferenceDto ToAssetReference(Asset asset) => new(
        asset.Name,
        asset.Symbol,
        asset.Isin,
        asset.MarketSymbol,
        asset.Currency);

    private static ImportedRuleResolution ResolveImportedRule(
        PortableStrategyRuleDto importedRule,
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Asset> assets)
    {
        var definition = importedRule.Definition;
        Guid? portfolioId = null;
        Guid? assetId = null;

        if (RequiresPortfolioReference(definition) || importedRule.Portfolio is not null)
        {
            var portfolio = ResolvePortfolio(importedRule.Portfolio, portfolios);
            if (portfolio is null)
            {
                return ImportedRuleResolution.Skipped($"Portefeuille introuvable pour la regle \"{importedRule.Name}\".");
            }

            portfolioId = portfolio.Id;
        }

        if (RequiresAssetReference(definition) || importedRule.Asset is not null)
        {
            var asset = ResolveAsset(importedRule.Asset, assets);
            if (asset is null)
            {
                return ImportedRuleResolution.Skipped($"Actif introuvable pour la regle \"{importedRule.Name}\".");
            }

            assetId = asset.Id;
        }

        definition = RebindDefinition(definition, portfolioId, assetId);

        return new ImportedRuleResolution(portfolioId, assetId, definition, null);
    }

    private static bool RequiresPortfolioReference(RuleDefinitionDto? definition) =>
        definition?.Target.Mode == RuleTargetMode.PortfolioAssets
        || definition?.Target is { Mode: RuleTargetMode.Specific, Type: RuleTargetType.Portfolio };

    private static bool RequiresAssetReference(RuleDefinitionDto? definition) =>
        definition?.Target is
        {
            Mode: RuleTargetMode.Specific,
            Type: RuleTargetType.Asset or RuleTargetType.Position
        };

    private static RuleDefinitionDto? RebindDefinition(RuleDefinitionDto? definition, Guid? portfolioId, Guid? assetId)
    {
        if (definition is null)
        {
            return null;
        }

        var target = definition.Target;
        var reboundTarget = target.Mode switch
        {
            RuleTargetMode.PortfolioAssets => target with { PortfolioId = portfolioId, AssetId = null },
            RuleTargetMode.Specific when target.Type == RuleTargetType.Portfolio => target with { PortfolioId = portfolioId, AssetId = null },
            RuleTargetMode.Specific when target.Type is RuleTargetType.Asset or RuleTargetType.Position => target with { PortfolioId = null, AssetId = assetId },
            _ => target with { PortfolioId = null, AssetId = null }
        };

        return definition with { Target = reboundTarget };
    }

    private static Portfolio? ResolvePortfolio(PortablePortfolioReferenceDto? reference, IReadOnlyList<Portfolio> portfolios)
    {
        if (reference is null)
        {
            return null;
        }

        var name = NormalizeLookup(reference.Name);
        var broker = NormalizeLookup(reference.Broker);
        var currency = NormalizeLookup(reference.BaseCurrency);

        return portfolios.FirstOrDefault(portfolio =>
                NormalizeLookup(portfolio.Name) == name
                && NormalizeLookup(portfolio.Broker) == broker
                && NormalizeLookup(portfolio.BaseCurrency) == currency)
            ?? portfolios.FirstOrDefault(portfolio => NormalizeLookup(portfolio.Name) == name);
    }

    private static Asset? ResolveAsset(PortableAssetReferenceDto? reference, IReadOnlyList<Asset> assets)
    {
        if (reference is null)
        {
            return null;
        }

        var isin = NormalizeLookup(reference.Isin);
        if (!string.IsNullOrWhiteSpace(isin))
        {
            var assetByIsin = assets.FirstOrDefault(asset => NormalizeLookup(asset.Isin) == isin);
            if (assetByIsin is not null)
            {
                return assetByIsin;
            }
        }

        var marketSymbol = NormalizeLookup(reference.MarketSymbol);
        if (!string.IsNullOrWhiteSpace(marketSymbol))
        {
            var assetByMarketSymbol = assets.FirstOrDefault(asset => NormalizeLookup(asset.MarketSymbol) == marketSymbol);
            if (assetByMarketSymbol is not null)
            {
                return assetByMarketSymbol;
            }
        }

        var symbol = NormalizeLookup(reference.Symbol);
        var currency = NormalizeLookup(reference.Currency);
        return assets.FirstOrDefault(asset => NormalizeLookup(asset.Symbol) == symbol && NormalizeLookup(asset.Currency) == currency)
            ?? assets.FirstOrDefault(asset => NormalizeLookup(asset.Name) == NormalizeLookup(reference.Name));
    }

    private static string NormalizeLookup(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    private static string NormalizeRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RuleDefinitionDto? ValidateDefinition(RuleDefinitionDto? definition)
    {
        if (definition is null)
        {
            return null;
        }

        if (definition.Version != 1)
        {
            throw new ArgumentException("La version de regle doit etre 1.", nameof(definition));
        }

        if (definition.Target.Mode == RuleTargetMode.Specific
            && definition.Target.Type is RuleTargetType.Asset or RuleTargetType.Position
            && definition.Target.AssetId is null)
        {
            throw new ArgumentException("Une regle ciblee sur un actif specifique doit renseigner un actif.", nameof(definition));
        }

        if (definition.Target.Mode == RuleTargetMode.Specific
            && definition.Target.Type == RuleTargetType.Portfolio
            && definition.Target.PortfolioId is null)
        {
            throw new ArgumentException("Une regle ciblee sur un portefeuille specifique doit renseigner un portefeuille.", nameof(definition));
        }

        if (definition.Target.Mode == RuleTargetMode.PortfolioAssets && definition.Target.PortfolioId is null)
        {
            throw new ArgumentException("Une regle ciblee sur les actifs d'un portefeuille doit renseigner un portefeuille.", nameof(definition));
        }

        if (definition.Condition.Metric != RuleConditionMetric.Always && definition.Condition.Value is null)
        {
            throw new ArgumentException("Une condition mesurable doit renseigner un seuil.", nameof(definition));
        }

        if (definition.Condition.Operator == RuleComparisonOperator.BetweenInclusive && definition.Condition.UpperValue is null)
        {
            throw new ArgumentException("Une condition entre deux seuils doit renseigner une borne haute.", nameof(definition));
        }

        if (definition.Condition.Metric == RuleConditionMetric.PriceChangePercent && definition.Condition.Period is null)
        {
            throw new ArgumentException("Une condition de variation de prix doit renseigner une periode.", nameof(definition));
        }

        if (string.IsNullOrWhiteSpace(definition.Effect.Message))
        {
            throw new ArgumentException("Une regle doit fournir un message exploitable.", nameof(definition));
        }

        return definition with
        {
            Effect = definition.Effect with { Message = definition.Effect.Message.Trim() }
        };
    }

    private static string? SerializeDefinition(RuleDefinitionDto? definition) =>
        definition is null ? null : JsonSerializer.Serialize(definition, JsonOptions);

    public static RuleDefinitionDto? DeserializeDefinition(string? definitionJson) =>
        string.IsNullOrWhiteSpace(definitionJson)
            ? null
            : JsonSerializer.Deserialize<RuleDefinitionDto>(definitionJson, JsonOptions);

    private sealed record ImportedRuleResolution(
        Guid? PortfolioId,
        Guid? AssetId,
        RuleDefinitionDto? Definition,
        string? SkipMessage)
    {
        public static ImportedRuleResolution Skipped(string message) => new(null, null, null, message);
    }
}
