using TradeCopilot.Application.Contracts.Strategy;

namespace TradeCopilot.Application.Services.Strategy;

public interface IStrategyService
{
    Task<StrategyDto> GetStrategyAsync(CancellationToken cancellationToken = default);
}
