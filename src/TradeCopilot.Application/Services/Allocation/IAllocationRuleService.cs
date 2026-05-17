using TradeCopilot.Application.Contracts.Allocation;

namespace TradeCopilot.Application.Services.Allocation;

public interface IAllocationRuleService
{
    Task<IReadOnlyList<AllocationRuleDto>> GetAllocationRulesAsync(CancellationToken cancellationToken = default);
    Task<AllocationRuleDto> CreateAllocationRuleAsync(CreateAllocationRuleRequest request, CancellationToken cancellationToken = default);
    Task<AllocationRuleDto?> UpdateAllocationRuleAsync(Guid id, UpdateAllocationRuleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAllocationRuleAsync(Guid id, CancellationToken cancellationToken = default);
}
