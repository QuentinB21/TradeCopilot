using TradeCopilot.Application.Abstractions;
using TradeCopilot.Application.Contracts.MarketData;

namespace TradeCopilot.Application.Services.MarketData;

public sealed class MarketDataService(IMarketDataProvider marketDataProvider) : IMarketDataService
{
    public Task<IReadOnlyList<InstrumentSearchResultDto>> SearchInstrumentsAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        return marketDataProvider.SearchInstrumentsAsync(query.Trim(), cancellationToken);
    }

    public Task<MarketQuoteDto?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return marketDataProvider.GetLatestQuoteAsync(symbol.Trim(), cancellationToken);
    }
}
