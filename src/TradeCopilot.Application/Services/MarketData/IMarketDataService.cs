using TradeCopilot.Application.Contracts.MarketData;

namespace TradeCopilot.Application.Services.MarketData;

public interface IMarketDataService
{
    Task<IReadOnlyList<InstrumentSearchResultDto>> SearchInstrumentsAsync(string query, CancellationToken cancellationToken = default);
    Task<MarketQuoteDto?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default);
}
