using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Common;
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
        await ValidateTargetWeightAsync(request.TargetWeight, null, cancellationToken);

        var portfolio = new Portfolio
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            Broker = request.Broker.Trim(),
            BaseCurrency = request.BaseCurrency.Trim().ToUpperInvariant(),
            CashBalance = request.CashBalance,
            Repartitions =
            [
                PortfolioRepartition(request.TargetWeight)
            ]
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

        await ValidateTargetWeightAsync(request.TargetWeight, portfolio.Id, cancellationToken);

        portfolio.Name = request.Name.Trim();
        portfolio.Type = request.Type;
        portfolio.Broker = request.Broker.Trim();
        portfolio.BaseCurrency = request.BaseCurrency.Trim().ToUpperInvariant();
        portfolio.CashBalance = request.CashBalance;
        var repartition = portfolio.Repartitions.SingleOrDefault(candidate => candidate.Kind == RepartitionKind.Portfolio);
        if (repartition is null)
        {
            portfolio.Repartitions.Add(PortfolioRepartition(request.TargetWeight));
        }
        else
        {
            repartition.TargetWeight = request.TargetWeight;
        }

        await repository.UpdatePortfolioAsync(portfolio, cancellationToken);
        return ToDto(portfolio);
    }

    public async Task<DeleteEntityResult> DeletePortfolioAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var portfolio = await repository.GetPortfolioByIdAsync(id, cancellationToken);
        if (portfolio is null)
        {
            return DeleteEntityResult.NotFound;
        }

        if (await repository.PortfolioHasReferencesAsync(id, cancellationToken))
        {
            return DeleteEntityResult.Conflict;
        }

        await repository.DeletePortfolioAsync(portfolio, cancellationToken);
        return DeleteEntityResult.Deleted;
    }

    private async Task ValidateTargetWeightAsync(decimal targetWeight, Guid? excludedPortfolioId, CancellationToken cancellationToken)
    {
        if (targetWeight is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(targetWeight), "Une cle globale doit rester comprise entre 0 et 100 %.");
        }

        var existingWeight = (await repository.GetPortfoliosAsync(cancellationToken))
            .Where(portfolio => portfolio.Id != excludedPortfolioId)
            .Sum(portfolio => portfolio.TargetWeight);

        if (existingWeight + targetWeight > 1.000001m)
        {
            throw new ArgumentException("La somme des cles globales des portefeuilles ne peut pas depasser 100 %.", nameof(targetWeight));
        }
    }

    private static PortfolioDto ToDto(Portfolio portfolio) => new(
        portfolio.Id,
        portfolio.Name,
        portfolio.Type,
        portfolio.Broker,
        portfolio.BaseCurrency,
        portfolio.CashBalance,
        portfolio.TargetWeight);

    private static Repartition PortfolioRepartition(decimal targetWeight) => new()
    {
        Kind = RepartitionKind.Portfolio,
        TargetWeight = targetWeight
    };
}
