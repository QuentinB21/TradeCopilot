using TradeCopilot.Application.Abstractions;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Prices;

public sealed class MarketPriceRefreshService(
    IInvestmentRepository repository,
    IMarketDataProvider marketDataProvider) : IMarketPriceRefreshService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
    private const string NormalizedSourceSuffix = ":normalized-v2";

    public async Task<IReadOnlyList<AssetPrice>> RefreshCurrentPricesAsync(
        IReadOnlyList<Asset> assets,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<AssetPrice> existingPrices,
        CancellationToken cancellationToken = default)
    {
        var heldAssetIds = GetHeldAssetIds(transactions);
        if (heldAssetIds.Count == 0)
        {
            return existingPrices;
        }

        var now = DateTimeOffset.UtcNow;
        var refreshedPrices = existingPrices.ToList();
        var latestPriceByAssetId = refreshedPrices
            .GroupBy(price => price.AssetId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(price => price.RetrievedAt).ThenByDescending(price => price.Date).First());

        var refreshableAssetIds = assets
            .Where(asset => asset.Type is AssetType.Stock or AssetType.Etf)
            .Select(asset => asset.Id)
            .ToHashSet();

        foreach (var asset in assets.Where(asset => heldAssetIds.Contains(asset.Id) && refreshableAssetIds.Contains(asset.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(asset.Symbol))
            {
                continue;
            }

            latestPriceByAssetId.TryGetValue(asset.Id, out var latestPrice);
            if (latestPrice is not null
                && latestPrice.Source.EndsWith(NormalizedSourceSuffix, StringComparison.OrdinalIgnoreCase)
                && now - latestPrice.RetrievedAt < RefreshInterval)
            {
                continue;
            }

            var targetCurrency = GetTargetCurrency(asset, transactions);
            var normalizedQuote = await GetNormalizedQuoteAsync(asset, targetCurrency, cancellationToken);
            if (normalizedQuote is null)
            {
                continue;
            }

            var existingQuoteForDate = refreshedPrices.FirstOrDefault(price => price.AssetId == asset.Id && price.Date == normalizedQuote.Date);
            if (existingQuoteForDate is null)
            {
                var newPrice = new AssetPrice
                {
                    AssetId = asset.Id,
                    Date = normalizedQuote.Date,
                    Open = normalizedQuote.Open,
                    High = normalizedQuote.High,
                    Low = normalizedQuote.Low,
                    Close = normalizedQuote.Close,
                    Currency = normalizedQuote.Currency,
                    Source = normalizedQuote.Source,
                    RetrievedAt = normalizedQuote.RetrievedAt
                };

                await repository.AddPriceAsync(newPrice, cancellationToken);
                refreshedPrices.Add(newPrice);
                latestPriceByAssetId[asset.Id] = newPrice;
                continue;
            }

            existingQuoteForDate.Open = normalizedQuote.Open;
            existingQuoteForDate.High = normalizedQuote.High;
            existingQuoteForDate.Low = normalizedQuote.Low;
            existingQuoteForDate.Close = normalizedQuote.Close;
            existingQuoteForDate.Currency = normalizedQuote.Currency;
            existingQuoteForDate.Source = normalizedQuote.Source;
            existingQuoteForDate.RetrievedAt = normalizedQuote.RetrievedAt;

            await repository.UpdatePriceAsync(existingQuoteForDate, cancellationToken);
            latestPriceByAssetId[asset.Id] = existingQuoteForDate;
        }

        return refreshedPrices
            .Where(price => refreshableAssetIds.Contains(price.AssetId)
                || !price.Source.StartsWith("yahoo-finance", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<NormalizedMarketQuote?> GetNormalizedQuoteAsync(Asset asset, string targetCurrency, CancellationToken cancellationToken)
    {
        var directQuote = await marketDataProvider.GetLatestQuoteAsync(asset.Symbol, cancellationToken);
        if (directQuote is not null)
        {
            return await NormalizeQuoteAsync(asset, directQuote, targetCurrency, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(asset.Isin))
        {
            return null;
        }

        var candidates = await marketDataProvider.SearchInstrumentsAsync(asset.Isin, cancellationToken);
        foreach (var candidate in candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Symbol))
            .DistinctBy(candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .Take(5))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var quote = await marketDataProvider.GetLatestQuoteAsync(candidate.Symbol, cancellationToken);
            if (quote is null)
            {
                continue;
            }

            var normalizedQuote = await NormalizeQuoteAsync(asset, quote, targetCurrency, cancellationToken);
            if (normalizedQuote is not null)
            {
                return normalizedQuote;
            }
        }

        return null;
    }

    private async Task<NormalizedMarketQuote?> NormalizeQuoteAsync(Asset asset, Contracts.MarketData.MarketQuoteDto quote, string targetCurrency, CancellationToken cancellationToken)
    {
        targetCurrency = targetCurrency.Trim().ToUpperInvariant();
        var quoteCurrency = quote.Currency.Trim().ToUpperInvariant();
        var open = quote.Open;
        var high = quote.High;
        var low = quote.Low;
        var close = quote.Close;

        if (asset.Symbol.EndsWith(".L", StringComparison.OrdinalIgnoreCase)
            && quoteCurrency == "GBP"
            && close > 100m)
        {
            open /= 100m;
            high /= 100m;
            low /= 100m;
            close /= 100m;
        }

        if (quoteCurrency != targetCurrency)
        {
            var fxQuote = await marketDataProvider.GetLatestQuoteAsync($"{quoteCurrency}{targetCurrency}=X", cancellationToken);
            if (fxQuote is null)
            {
                return null;
            }

            open *= fxQuote.Close;
            high *= fxQuote.Close;
            low *= fxQuote.Close;
            close *= fxQuote.Close;
            quoteCurrency = targetCurrency;
        }

        return new NormalizedMarketQuote(
            quote.Date,
            Round(open),
            Round(high),
            Round(low),
            Round(close) ?? 0m,
            quoteCurrency,
            $"{quote.Provider}{NormalizedSourceSuffix}",
            DateTimeOffset.UtcNow);
    }

    private static HashSet<Guid> GetHeldAssetIds(IReadOnlyList<Transaction> transactions)
    {
        var quantitiesByAssetId = new Dictionary<Guid, decimal>();
        foreach (var group in transactions
            .Where(transaction => transaction.AssetId.HasValue)
            .GroupBy(transaction => transaction.AssetId!.Value))
        {
            var quantity = 0m;
            foreach (var transaction in group.OrderBy(transaction => transaction.Date))
            {
                switch (transaction.Type)
                {
                    case TransactionType.Buy:
                        quantity += transaction.Quantity;
                        break;
                    case TransactionType.Sell:
                        quantity -= Math.Min(transaction.Quantity, quantity);
                        break;
                    case TransactionType.Split:
                        if (transaction.Quantity > 0m)
                        {
                            quantity *= transaction.Quantity;
                        }
                        break;
                }
            }

            quantitiesByAssetId[group.Key] = quantity;
        }

        return quantitiesByAssetId
            .Where(item => item.Value > 0m)
            .Select(item => item.Key)
            .ToHashSet();
    }

    private static string GetTargetCurrency(Asset asset, IReadOnlyList<Transaction> transactions)
    {
        var transactionCurrency = transactions
            .Where(transaction => transaction.AssetId == asset.Id && !string.IsNullOrWhiteSpace(transaction.Currency))
            .GroupBy(transaction => transaction.Currency.Trim().ToUpperInvariant())
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();

        return transactionCurrency ?? asset.Currency.Trim().ToUpperInvariant();
    }

    private static decimal? Round(decimal? value) =>
        value.HasValue ? decimal.Round(value.Value, 6, MidpointRounding.AwayFromZero) : null;

    private sealed record NormalizedMarketQuote(
        DateOnly Date,
        decimal? Open,
        decimal? High,
        decimal? Low,
        decimal Close,
        string Currency,
        string Source,
        DateTimeOffset RetrievedAt);
}
