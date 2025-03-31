using QuantAssembly.Impl.IBGW;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Logging;
using QuantAssembly.Utility;

namespace QuantAssembly.DataProvider
{
    public class IBGWMarketDataProvider : IMarketDataProvider
    {
        private readonly IIBGWClient ibgwClient;
        private readonly ILogger logger;
        private readonly Dictionary<string, MarketData> marketDataCache = new Dictionary<string, MarketData>();
        private readonly object lockObj = new object();

        public IBGWMarketDataProvider(IIBGWClient ibgwClient, ILogger logger)
        {
            this.ibgwClient = ibgwClient;
            this.logger = logger;
        }

        public void FlushMarketDataCache()
        {
            logger.LogDebug("[IBGWMarketDataProvider] Flushing market data cache.");
            marketDataCache.Clear();
        }

        public async Task<MarketData> GetMarketDataAsync(string ticker)
        {
            lock (lockObj)
            {
                if (marketDataCache.TryGetValue(ticker, out var marketData))
                {
                    logger.LogDebug($"[IBGWMarketDataProvider::GetMarketDataAsync] Fetching market data for ticker: {ticker} from cache.");
                    return marketData;
                }
            }

            logger.LogDebug($"[IBGWMarketDataProvider::GetMarketDataAsync] Fetching market data for ticker: {ticker} from IBGWClient since it is not cached.");
            var marketDataFresh = await ibgwClient.RequestMarketDataAsync(ticker); 

            lock (lockObj)
            {
                marketDataCache[ticker] = marketDataFresh;
            }

            return marketDataFresh;
        }
    }
}