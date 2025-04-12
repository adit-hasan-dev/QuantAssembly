using QuantAssembly.Common.Models;

namespace QuantAssembly.Core.DataProvider
{
    public interface IMarketDataProvider
    {
        Task<MarketData> GetMarketDataAsync(string symbol);
        public void FlushMarketDataCache();
    }
}
