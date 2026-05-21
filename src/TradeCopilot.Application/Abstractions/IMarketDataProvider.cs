using TradeCopilot.Application.Contracts.MarketData;

namespace TradeCopilot.Application.Abstractions;

public interface IMarketDataProvider
{
    Task<IReadOnlyList<InstrumentSearchResultDto>> SearchInstrumentsAsync(string query, CancellationToken cancellationToken = default);
    Task<MarketQuoteDto?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MarketQuoteDto>> GetDailyQuotesAsync(string symbol, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
