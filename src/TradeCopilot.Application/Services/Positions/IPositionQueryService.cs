using TradeCopilot.Application.Contracts.Positions;

namespace TradeCopilot.Application.Services.Positions;

public interface IPositionQueryService
{
    Task<IReadOnlyList<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default);
}
