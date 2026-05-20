using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.Imports;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Imports;

public sealed class TransactionImportService(
    IInvestmentRepository repository,
    IEnumerable<ITransactionImportStrategy> strategies) : ITransactionImportService
{
    public async Task<TransactionImportResultDto?> ImportAsync(
        TransactionImportRequest request,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await repository.GetPortfolioByIdAsync(request.PortfolioId, cancellationToken);
        if (portfolio is null)
        {
            return null;
        }

        var strategy = strategies.FirstOrDefault(candidate => candidate.Provider == request.Provider)
            ?? throw new NotSupportedException($"Import provider '{request.Provider}' is not supported.");

        var parseResult = await strategy.ParseAsync(stream, cancellationToken);
        var importSource = request.Provider.ToString();
        var knownExternalIds = new HashSet<string>(
            await repository.GetImportedTransactionExternalIdsAsync(importSource, cancellationToken),
            StringComparer.OrdinalIgnoreCase);
        var assets = (await repository.GetAssetsAsync(cancellationToken)).ToList();
        var createdAssets = 0;
        var duplicateRows = 0;
        var importedTransactions = new List<Transaction>();
        var warnings = parseResult.Warnings.ToList();

        foreach (var candidate in parseResult.Transactions)
        {
            if (knownExternalIds.Contains(candidate.ExternalId))
            {
                duplicateRows++;
                continue;
            }

            var asset = candidate.Asset is null
                ? null
                : await ResolveAssetAsync(candidate.Asset, assets, cancellationToken);

            if (asset is not null && asset.Id == Guid.Empty)
            {
                asset.Id = Guid.NewGuid();
                await repository.AddAssetAsync(asset, cancellationToken);
                assets.Add(asset);
                createdAssets++;
            }

            importedTransactions.Add(new Transaction
            {
                PortfolioId = portfolio.Id,
                AssetId = asset?.Id,
                Type = candidate.Type,
                Date = candidate.Date,
                Quantity = candidate.Quantity,
                UnitPrice = candidate.UnitPrice,
                Fees = candidate.Fees,
                Currency = candidate.Currency,
                Comment = candidate.Comment,
                ImportSource = importSource,
                ExternalId = candidate.ExternalId
            });

            knownExternalIds.Add(candidate.ExternalId);
        }

        if (importedTransactions.Count > 0)
        {
            await repository.AddTransactionsAsync(importedTransactions, cancellationToken);
        }

        var skippedRows = parseResult.RowsRead - parseResult.Transactions.Count + duplicateRows;
        return new TransactionImportResultDto(
            parseResult.RowsRead,
            importedTransactions.Count,
            createdAssets,
            duplicateRows,
            skippedRows,
            warnings);
    }

    private static Task<Asset> ResolveAssetAsync(
        ImportedAssetCandidate candidate,
        List<Asset> assets,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = assets.FirstOrDefault(asset =>
            (!string.IsNullOrWhiteSpace(candidate.Isin) && string.Equals(asset.Isin, candidate.Isin, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(asset.Symbol, candidate.Symbol, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        return Task.FromResult(new Asset
        {
            Id = Guid.Empty,
            Name = candidate.Name,
            Symbol = candidate.Symbol,
            Isin = candidate.Isin,
            Type = candidate.Type,
            Currency = candidate.Currency,
            PriceProvider = "import",
            StrategicStatus = StrategicStatus.Observation
        });
    }
}
