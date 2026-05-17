using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Allocation;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Allocation;

public sealed class AllocationRuleService(IInvestmentRepository repository) : IAllocationRuleService
{
    public async Task<IReadOnlyList<AllocationRuleDto>> GetAllocationRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await repository.GetAllocationRulesAsync(cancellationToken);
        return rules
            .OrderBy(rule => rule.PortfolioId)
            .ThenBy(rule => rule.AssetId)
            .Select(ToDto)
            .ToList();
    }

    public async Task<AllocationRuleDto> CreateAllocationRuleAsync(CreateAllocationRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = new AllocationRule
        {
            PortfolioId = request.PortfolioId,
            AssetId = request.AssetId,
            TargetWeight = request.TargetWeight,
            MinWeight = request.MinWeight,
            MaxWeight = request.MaxWeight,
            Status = request.Status
        };

        await repository.AddAllocationRuleAsync(rule, cancellationToken);
        return ToDto(rule);
    }

    public async Task<AllocationRuleDto?> UpdateAllocationRuleAsync(Guid id, UpdateAllocationRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetAllocationRuleByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        rule.TargetWeight = request.TargetWeight;
        rule.MinWeight = request.MinWeight;
        rule.MaxWeight = request.MaxWeight;
        rule.Status = request.Status;

        await repository.UpdateAllocationRuleAsync(rule, cancellationToken);
        return ToDto(rule);
    }

    public async Task<bool> DeleteAllocationRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetAllocationRuleByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        await repository.DeleteAllocationRuleAsync(rule, cancellationToken);
        return true;
    }

    private static AllocationRuleDto ToDto(AllocationRule rule) => new(
        rule.Id,
        rule.PortfolioId,
        rule.AssetId,
        rule.TargetWeight,
        rule.MinWeight,
        rule.MaxWeight,
        rule.Status);
}
