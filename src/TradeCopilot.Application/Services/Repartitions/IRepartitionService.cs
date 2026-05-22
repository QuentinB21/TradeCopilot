using TradeCopilot.Application.Contracts.Repartitions;

namespace TradeCopilot.Application.Services.Repartitions;

public interface IRepartitionService
{
    Task<IReadOnlyList<RepartitionDto>> GetAssetRepartitionsAsync(CancellationToken cancellationToken = default);
    Task<RepartitionDto> CreateAssetRepartitionAsync(CreateRepartitionRequest request, CancellationToken cancellationToken = default);
    Task<RepartitionDto?> UpdateAssetRepartitionAsync(Guid id, UpdateRepartitionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAssetRepartitionAsync(Guid id, CancellationToken cancellationToken = default);
}
