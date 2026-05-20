using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Imports;

public sealed record ImportedAssetCandidate(
    string Name,
    string Symbol,
    string? Isin,
    AssetType Type,
    string Currency);
