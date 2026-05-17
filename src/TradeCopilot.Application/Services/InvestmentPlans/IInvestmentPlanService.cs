using TradeCopilot.Application.Contracts.InvestmentPlans;

namespace TradeCopilot.Application.Services.InvestmentPlans;

public interface IInvestmentPlanService
{
    Task<MonthlyInvestmentPlanDto> BuildMonthlyPlanAsync(MonthlyInvestmentRequest request, CancellationToken cancellationToken = default);
}
