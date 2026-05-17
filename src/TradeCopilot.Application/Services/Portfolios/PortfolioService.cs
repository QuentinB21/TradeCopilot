using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Portfolios;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Portfolios;

public sealed class PortfolioService(IInvestmentRepository repository) : IPortfolioService
{
    public async Task<IReadOnlyList<PortfolioDto>> GetPortfoliosAsync(CancellationToken cancellationToken = default)
    {
        var portfolios = await repository.GetPortfoliosAsync(cancellationToken);
        return portfolios
            .OrderBy(portfolio => portfolio.Name)
            .Select(ToDto)
            .ToList();
    }

    public async Task<PortfolioDto?> GetPortfolioAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var portfolio = await repository.GetPortfolioByIdAsync(id, cancellationToken);
        return portfolio is null ? null : ToDto(portfolio);
    }

    public async Task<PortfolioDto> CreatePortfolioAsync(CreatePortfolioRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Broker);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BaseCurrency);

        var portfolio = new Portfolio
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            Broker = request.Broker.Trim(),
            BaseCurrency = request.BaseCurrency.Trim().ToUpperInvariant(),
            CashBalance = request.CashBalance
        };

        await repository.AddPortfolioAsync(portfolio, cancellationToken);
        return ToDto(portfolio);
    }

    public async Task<PortfolioDto?> UpdatePortfolioAsync(Guid id, UpdatePortfolioRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Broker);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BaseCurrency);

        var portfolio = await repository.GetPortfolioByIdAsync(id, cancellationToken);
        if (portfolio is null)
        {
            return null;
        }

        portfolio.Name = request.Name.Trim();
        portfolio.Type = request.Type;
        portfolio.Broker = request.Broker.Trim();
        portfolio.BaseCurrency = request.BaseCurrency.Trim().ToUpperInvariant();
        portfolio.CashBalance = request.CashBalance;

        await repository.UpdatePortfolioAsync(portfolio, cancellationToken);
        return ToDto(portfolio);
    }

    public async Task<bool> DeletePortfolioAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var portfolio = await repository.GetPortfolioByIdAsync(id, cancellationToken);
        if (portfolio is null)
        {
            return false;
        }

        await repository.DeletePortfolioAsync(portfolio, cancellationToken);
        return true;
    }

    private static PortfolioDto ToDto(Portfolio portfolio) => new(
        portfolio.Id,
        portfolio.Name,
        portfolio.Type,
        portfolio.Broker,
        portfolio.BaseCurrency,
        portfolio.CashBalance);
}
