using QuantAssembly.Common.Models;

namespace QuantAssembly.DataProvider
{
    public interface IMarketDataProvider
    {
        Task<MarketData> GetMarketDataAsync(string symbol);
        Task<bool> IsWithinTradingHours(string symbol, DateTime? dateTime);
        public void FlushMarketDataCache();
    }
}
