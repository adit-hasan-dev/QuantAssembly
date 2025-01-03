using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public interface IMarketDataProvider
    {
        Task<MarketData> GetMarketDataAsync(string symbol);
        Task<bool> IsWithinTradingHours(string symbol, DateTime? dateTime);
    }
}
