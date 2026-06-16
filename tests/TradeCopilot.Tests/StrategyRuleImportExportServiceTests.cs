using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Strategy;
using TradeCopilot.Application.Services.Strategy;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class StrategyRuleImportExportServiceTests
{
    [Fact]
    public async Task Exports_specific_asset_rule_with_portable_reference_without_internal_definition_id()
    {
        var assetId = Guid.NewGuid();
        var repository = new FakeInvestmentRepository(
            [],
            [
                new Asset
                {
                    Id = assetId,
                    Name = "Air Liquide",
                    Symbol = "AI",
                    Isin = "FR0000120073",
                    Type = AssetType.Stock,
                    Currency = "EUR",
                    MarketSymbol = "AI.PA",
                    StrategicStatus = StrategicStatus.Core
                }
            ]);
        repository.StrategyRules.Add(new StrategyRule
        {
            Name = "Renforcer Air Liquide",
            Description = "Regle portable.",
            RecommendedAction = "Verifier la ligne.",
            AssetId = assetId,
            DefinitionJson = System.Text.Json.JsonSerializer.Serialize(SpecificAssetDefinition(assetId), JsonOptions),
            Priority = 10,
            IsActive = true
        });
        var service = new StrategyRuleService(repository);

        var exportFile = await service.ExportStrategyRulesAsync();

        var exportedRule = Assert.Single(exportFile.Rules);
        Assert.Equal("AI", exportedRule.Asset?.Symbol);
        Assert.Null(exportedRule.Definition?.Target.AssetId);
    }

    [Fact]
    public async Task Imports_specific_asset_rule_by_matching_local_asset_reference()
    {
        var sourceAssetId = Guid.NewGuid();
        var localAssetId = Guid.NewGuid();
        var repository = new FakeInvestmentRepository(
            [],
            [
                new Asset
                {
                    Id = localAssetId,
                    Name = "Air Liquide",
                    Symbol = "AI",
                    Isin = "FR0000120073",
                    Type = AssetType.Stock,
                    Currency = "EUR",
                    MarketSymbol = "AI.PA",
                    StrategicStatus = StrategicStatus.Core
                }
            ]);
        var service = new StrategyRuleService(repository);

        var result = await service.ImportStrategyRulesAsync(new StrategyRulesExportDto(
            "tradecopilot.strategy-rules",
            1,
            DateTimeOffset.UtcNow,
            [
                new PortableStrategyRuleDto(
                    "Renforcer Air Liquide",
                    "Regle portable.",
                    null,
                    "Verifier la ligne.",
                    SpecificAssetDefinition(sourceAssetId),
                    10,
                    true,
                    null,
                    new PortableAssetReferenceDto("Air Liquide", "AI", "FR0000120073", "AI.PA", "EUR"))
            ]));

        Assert.Equal(1, result.ImportedRules);
        Assert.Equal(0, result.SkippedRules);
        var importedRule = Assert.Single(repository.StrategyRules);
        Assert.Equal(localAssetId, importedRule.AssetId);
        Assert.Equal(localAssetId, StrategyRuleService.DeserializeDefinition(importedRule.DefinitionJson)?.Target.AssetId);
    }

    [Fact]
    public async Task Skips_specific_asset_rule_when_target_asset_is_missing()
    {
        var repository = new FakeInvestmentRepository([], []);
        var service = new StrategyRuleService(repository);

        var result = await service.ImportStrategyRulesAsync(new StrategyRulesExportDto(
            "tradecopilot.strategy-rules",
            1,
            DateTimeOffset.UtcNow,
            [
                new PortableStrategyRuleDto(
                    "Renforcer Air Liquide",
                    "Regle portable.",
                    null,
                    "Verifier la ligne.",
                    SpecificAssetDefinition(Guid.NewGuid()),
                    10,
                    true,
                    null,
                    new PortableAssetReferenceDto("Air Liquide", "AI", "FR0000120073", "AI.PA", "EUR"))
            ]));

        Assert.Equal(0, result.ImportedRules);
        Assert.Equal(1, result.SkippedRules);
        Assert.Empty(repository.StrategyRules);
        Assert.Equal("MissingReference", Assert.Single(result.Warnings).Code);
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static RuleDefinitionDto SpecificAssetDefinition(Guid assetId) => new(
        1,
        new RuleTargetDto(RuleTargetType.Asset, RuleTargetMode.Specific, null, assetId),
        new RuleConditionDto(
            RuleConditionMetric.PriceChangePercent,
            RuleComparisonOperator.LessThanOrEqual,
            -0.05m,
            null,
            RuleValueUnit.Percent,
            new RulePeriodDto(1, RuleTimeUnit.Month)),
        new RuleEffectDto(RuleEffectType.RequireReview, RuleEffectStrength.Soft, RuleSeverity.Warning, "Verifier la ligne."));

    private sealed class FakeInvestmentRepository(IReadOnlyList<Portfolio> portfolios, IReadOnlyList<Asset> assets) : IInvestmentRepository
    {
        public List<StrategyRule> StrategyRules { get; } = [];

        public Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken cancellationToken = default) => Task.FromResult(portfolios);
        public Task<Portfolio?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> PortfolioHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Asset>> GetAssetsAsync(CancellationToken cancellationToken = default) => Task.FromResult(assets);
        public Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AssetHasReferencesAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlySet<string>> GetImportedTransactionExternalIdsAsync(string importSource, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AssetPrice>> GetPricesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AssetPrice?> GetPriceByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Repartition>> GetAssetRepartitionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Repartition?> GetAssetRepartitionByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StrategyRule>> GetStrategyRulesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<StrategyRule>>(StrategyRules);
        public Task<StrategyRule?> GetStrategyRuleByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddPortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAssetAsync(Asset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddTransactionsAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddPriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdatePriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePriceAsync(AssetPrice price, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteRepartitionAsync(Repartition repartition, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default)
        {
            StrategyRules.Add(strategyRule);
            return Task.CompletedTask;
        }

        public Task UpdateStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteStrategyRuleAsync(StrategyRule strategyRule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
