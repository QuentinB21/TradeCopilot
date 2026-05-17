using TradeCopilot.Application.Contracts.Portfolios;

namespace TradeCopilot.Application.Services.Portfolios;

public interface IPortfolioService
{
    Task<IReadOnlyList<PortfolioDto>> GetPortfoliosAsync(CancellationToken cancellationToken = default);
    Task<PortfolioDto?> GetPortfolioAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PortfolioDto> CreatePortfolioAsync(CreatePortfolioRequest request, CancellationToken cancellationToken = default);
    Task<PortfolioDto?> UpdatePortfolioAsync(Guid id, UpdatePortfolioRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePortfolioAsync(Guid id, CancellationToken cancellationToken = default);
}
