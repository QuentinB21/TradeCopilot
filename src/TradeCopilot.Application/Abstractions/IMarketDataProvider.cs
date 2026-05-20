using TradeCopilot.Application.Contracts.MarketData;

namespace TradeCopilot.Application.Abstractions;

public interface IMarketDataProvider
{
    Task<IReadOnlyList<InstrumentSearchResultDto>> SearchInstrumentsAsync(string query, CancellationToken cancellationToken = default);
    Task<MarketQuoteDto?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default);
}
