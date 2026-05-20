using System.Globalization;
using System.Text.Json;
using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.MarketData;
using TradeCopilot.Domain;

namespace TradeCopilot.Infrastructure.MarketData;

public sealed class YahooFinanceMarketDataProvider(HttpClient httpClient) : IMarketDataProvider
{
    private const string ProviderName = "yahoo-finance";

    public async Task<IReadOnlyList<InstrumentSearchResultDto>> SearchInstrumentsAsync(string query, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"v1/finance/search?q={Uri.EscapeDataString(query)}&quotesCount=10&newsCount=0",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("quotes", out var quotes) || quotes.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<InstrumentSearchResultDto>();
        foreach (var quote in quotes.EnumerateArray())
        {
            var symbol = GetString(quote, "symbol");
            var name = GetString(quote, "longname") ?? GetString(quote, "shortname");
            if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var quoteType = GetString(quote, "quoteType") ?? GetString(quote, "typeDisp");
            results.Add(new InstrumentSearchResultDto(
                symbol,
                name,
                GetString(quote, "exchange"),
                GetString(quote, "exchDisp"),
                quoteType,
                null,
                GetString(quote, "sector"),
                ToAssetType(quoteType),
                ProviderName));
        }

        return results
            .DistinctBy(result => result.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<MarketQuoteDto?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=5d",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var chart = document.RootElement.GetProperty("chart");
        if (!chart.TryGetProperty("result", out var results) || results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
        {
            return null;
        }

        var result = results[0];
        var meta = result.GetProperty("meta");
        var currency = GetString(meta, "currency") ?? "EUR";
        var marketPrice = GetDecimal(meta, "regularMarketPrice");
        if (marketPrice is null)
        {
            return null;
        }

        var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(GetInt64(meta, "regularMarketTime") ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).UtcDateTime);
        var quote = result.GetProperty("indicators").GetProperty("quote")[0];

        return new MarketQuoteDto(
            symbol.ToUpperInvariant(),
            date,
            LastDecimal(quote, "open"),
            LastDecimal(quote, "high"),
            LastDecimal(quote, "low"),
            marketPrice.Value,
            currency.ToUpperInvariant(),
            ProviderName,
            DateTimeOffset.UtcNow);
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static long? GetInt64(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value)
            ? value
            : null;

    private static decimal? GetDecimal(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetDecimal(out var value)
            ? value
            : null;

    private static decimal? LastDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        decimal? latest = null;
        foreach (var value in values.EnumerateArray())
        {
            if (value.ValueKind == JsonValueKind.Number)
            {
                latest = decimal.Parse(value.GetRawText(), CultureInfo.InvariantCulture);
            }
        }

        return latest;
    }

    private static AssetType ToAssetType(string? quoteType) =>
        quoteType?.ToUpperInvariant() switch
        {
            "ETF" => AssetType.Etf,
            "MUTUALFUND" => AssetType.Etf,
            "EQUITY" => AssetType.Stock,
            "CRYPTOCURRENCY" => AssetType.Other,
            _ => AssetType.Stock
        };
}
