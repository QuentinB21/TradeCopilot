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
            .Where(IsRefreshable)
            .Select(asset => asset.Id)
            .ToHashSet();

        await BackfillHistoricalPricesAsync(assets, transactions, refreshedPrices, refreshableAssetIds, cancellationToken);

        foreach (var asset in assets.Where(asset => heldAssetIds.Contains(asset.Id) && refreshableAssetIds.Contains(asset.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(asset.Symbol) && string.IsNullOrWhiteSpace(asset.MarketSymbol))
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

    private async Task BackfillHistoricalPricesAsync(
        IReadOnlyList<Asset> assets,
        IReadOnlyList<Transaction> transactions,
        List<AssetPrice> refreshedPrices,
        IReadOnlySet<Guid> refreshableAssetIds,
        CancellationToken cancellationToken)
    {
        var lastClosedDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var firstTransactionDateByAssetId = transactions
            .Where(transaction => transaction.AssetId.HasValue && refreshableAssetIds.Contains(transaction.AssetId.Value))
            .GroupBy(transaction => transaction.AssetId!.Value)
            .ToDictionary(group => group.Key, group => group.Min(transaction => transaction.Date));

        foreach (var asset in assets.Where(asset => firstTransactionDateByAssetId.ContainsKey(asset.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var from = firstTransactionDateByAssetId[asset.Id];
            if (from > lastClosedDate || HasUsefulHistoricalCoverage(asset.Id, from, lastClosedDate, refreshedPrices))
            {
                continue;
            }

            var targetCurrency = GetTargetCurrency(asset, transactions);
            var dailyQuotes = await GetHistoricalQuotesAsync(asset, from, lastClosedDate, cancellationToken);
            var normalizedQuotes = await NormalizeHistoricalQuotesAsync(dailyQuotes, targetCurrency, cancellationToken);

            foreach (var normalizedQuote in normalizedQuotes)
            {
                await UpsertQuoteAsync(asset.Id, normalizedQuote, refreshedPrices, cancellationToken);
            }
        }
    }

    private async Task<NormalizedMarketQuote?> GetNormalizedQuoteAsync(Asset asset, string targetCurrency, CancellationToken cancellationToken)
    {
        var quoteSymbol = string.IsNullOrWhiteSpace(asset.MarketSymbol) ? asset.Symbol : asset.MarketSymbol;
        var directQuote = await marketDataProvider.GetLatestQuoteAsync(quoteSymbol, cancellationToken);
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

    private async Task<IReadOnlyList<Contracts.MarketData.MarketQuoteDto>> GetHistoricalQuotesAsync(
        Asset asset,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var quoteSymbol = string.IsNullOrWhiteSpace(asset.MarketSymbol) ? asset.Symbol : asset.MarketSymbol;
        var directQuotes = await marketDataProvider.GetDailyQuotesAsync(quoteSymbol, from, to, cancellationToken);
        if (directQuotes.Count > 0 || string.IsNullOrWhiteSpace(asset.Isin))
        {
            return directQuotes;
        }

        var candidates = await marketDataProvider.SearchInstrumentsAsync(asset.Isin, cancellationToken);
        foreach (var candidate in candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Symbol))
            .DistinctBy(candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .Take(5))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var quotes = await marketDataProvider.GetDailyQuotesAsync(candidate.Symbol, from, to, cancellationToken);
            if (quotes.Count > 0)
            {
                return quotes;
            }
        }

        return [];
    }

    private async Task<NormalizedMarketQuote?> NormalizeQuoteAsync(Asset asset, Contracts.MarketData.MarketQuoteDto quote, string targetCurrency, CancellationToken cancellationToken)
    {
        targetCurrency = targetCurrency.Trim().ToUpperInvariant();
        var quoteCurrency = quote.Currency.Trim().ToUpperInvariant();
        var open = quote.Open;
        var high = quote.High;
        var low = quote.Low;
        var close = quote.Close;

        if (quote.Symbol.EndsWith(".L", StringComparison.OrdinalIgnoreCase)
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

    private async Task<IReadOnlyList<NormalizedMarketQuote>> NormalizeHistoricalQuotesAsync(
        IReadOnlyList<Contracts.MarketData.MarketQuoteDto> quotes,
        string targetCurrency,
        CancellationToken cancellationToken)
    {
        if (quotes.Count == 0)
        {
            return [];
        }

        targetCurrency = targetCurrency.Trim().ToUpperInvariant();
        var quoteCurrency = quotes[0].Currency.Trim().ToUpperInvariant();
        var fxQuotes = quoteCurrency == targetCurrency
            ? []
            : await marketDataProvider.GetDailyQuotesAsync(
                $"{quoteCurrency}{targetCurrency}=X",
                quotes.Min(quote => quote.Date),
                quotes.Max(quote => quote.Date),
                cancellationToken);

        var normalizedQuotes = new List<NormalizedMarketQuote>();
        foreach (var quote in quotes.OrderBy(quote => quote.Date))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fxRate = quoteCurrency == targetCurrency
                ? 1m
                : FindFxRate(fxQuotes, quote.Date);
            if (fxRate is null)
            {
                continue;
            }

            normalizedQuotes.Add(NormalizeHistoricalQuote(quote, targetCurrency, fxRate.Value));
        }

        return normalizedQuotes;
    }

    private static NormalizedMarketQuote NormalizeHistoricalQuote(
        Contracts.MarketData.MarketQuoteDto quote,
        string targetCurrency,
        decimal fxRate)
    {
        var open = quote.Open;
        var high = quote.High;
        var low = quote.Low;
        var close = quote.Close;

        if (quote.Symbol.EndsWith(".L", StringComparison.OrdinalIgnoreCase)
            && quote.Currency.Trim().Equals("GBP", StringComparison.OrdinalIgnoreCase)
            && close > 100m)
        {
            open /= 100m;
            high /= 100m;
            low /= 100m;
            close /= 100m;
        }

        return new NormalizedMarketQuote(
            quote.Date,
            Round(open * fxRate),
            Round(high * fxRate),
            Round(low * fxRate),
            Round(close * fxRate) ?? 0m,
            targetCurrency,
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

    private static bool HasUsefulHistoricalCoverage(
        Guid assetId,
        DateOnly from,
        DateOnly lastClosedDate,
        IReadOnlyList<AssetPrice> prices)
    {
        var historicalDates = prices
            .Where(price => price.AssetId == assetId
                && price.Date <= lastClosedDate
                && price.Source.EndsWith(NormalizedSourceSuffix, StringComparison.OrdinalIgnoreCase))
            .Select(price => price.Date)
            .OrderBy(date => date)
            .ToList();

        return historicalDates.Count > 0
            && historicalDates[0] <= from.AddDays(7)
            && historicalDates[^1] >= lastClosedDate.AddDays(-7);
    }

    private async Task UpsertQuoteAsync(
        Guid assetId,
        NormalizedMarketQuote normalizedQuote,
        List<AssetPrice> refreshedPrices,
        CancellationToken cancellationToken)
    {
        var existingQuoteForDate = refreshedPrices.FirstOrDefault(price => price.AssetId == assetId && price.Date == normalizedQuote.Date);
        if (existingQuoteForDate is null)
        {
            var newPrice = new AssetPrice
            {
                AssetId = assetId,
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
            return;
        }

        existingQuoteForDate.Open = normalizedQuote.Open;
        existingQuoteForDate.High = normalizedQuote.High;
        existingQuoteForDate.Low = normalizedQuote.Low;
        existingQuoteForDate.Close = normalizedQuote.Close;
        existingQuoteForDate.Currency = normalizedQuote.Currency;
        existingQuoteForDate.Source = normalizedQuote.Source;
        existingQuoteForDate.RetrievedAt = normalizedQuote.RetrievedAt;
        await repository.UpdatePriceAsync(existingQuoteForDate, cancellationToken);
    }

    private static decimal? FindFxRate(IReadOnlyList<Contracts.MarketData.MarketQuoteDto> fxQuotes, DateOnly quoteDate) =>
        fxQuotes
            .Where(fxQuote => fxQuote.Date <= quoteDate)
            .OrderByDescending(fxQuote => fxQuote.Date)
            .Select(fxQuote => (decimal?)fxQuote.Close)
            .FirstOrDefault();

    private static bool IsRefreshable(Asset asset) =>
        asset.Type is AssetType.Stock or AssetType.Etf
        || asset.Type is not AssetType.Cash && !string.IsNullOrWhiteSpace(asset.MarketSymbol);

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
