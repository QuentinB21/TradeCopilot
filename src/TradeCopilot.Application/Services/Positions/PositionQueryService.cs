using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Positions;

namespace TradeCopilot.Application.Services.Positions;

public sealed class PositionQueryService(IInvestmentRepository repository, PositionCalculator calculator) : IPositionQueryService
{
    public async Task<IReadOnlyList<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default) =>
        calculator.Calculate(
            await repository.GetPortfoliosAsync(cancellationToken),
            await repository.GetAssetsAsync(cancellationToken),
            await repository.GetTransactionsAsync(cancellationToken),
            await repository.GetPricesAsync(cancellationToken),
            await repository.GetAllocationRulesAsync(cancellationToken));
}
