using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradeCopilot.Application.Services.Strategy;

public sealed class StrategyRuleService(IInvestmentRepository repository) : IStrategyRuleService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<StrategyRuleDto>> GetStrategyRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await repository.GetStrategyRulesAsync(cancellationToken);
        return rules.OrderBy(rule => rule.Priority).ThenBy(rule => rule.Name).Select(ToDto).ToList();
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
}
