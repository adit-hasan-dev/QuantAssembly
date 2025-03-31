using QuantAssembly.Common.Models;

namespace QuantAssembly.DataProvider
{
    public interface IMarketDataProvider
    {
        Task<MarketData> GetMarketDataAsync(string symbol);
        public void FlushMarketDataCache();
    }
}
