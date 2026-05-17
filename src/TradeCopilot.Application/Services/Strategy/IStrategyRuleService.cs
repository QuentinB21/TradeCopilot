using TradeCopilot.Application.Contracts.Strategy;

namespace TradeCopilot.Application.Services.Strategy;

public interface IStrategyRuleService
{
    Task<IReadOnlyList<StrategyRuleDto>> GetStrategyRulesAsync(CancellationToken cancellationToken = default);
    Task<StrategyRuleDto> CreateStrategyRuleAsync(CreateStrategyRuleRequest request, CancellationToken cancellationToken = default);
    Task<StrategyRuleDto?> UpdateStrategyRuleAsync(Guid id, UpdateStrategyRuleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteStrategyRuleAsync(Guid id, CancellationToken cancellationToken = default);
}
