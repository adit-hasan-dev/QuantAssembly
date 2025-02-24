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

        public async Task<bool> IsWithinTradingHours(string ticker, DateTime? dateTime)
        {
            var tradingHours = await CacheWrapper.WithCacheAsync(ticker, async () =>
            {
                var contractDetails = await ibgwClient.GetSymbolContractDetailsAsync(ticker);
                var tradingHours = ParseTradingHours(contractDetails.TradingHours, contractDetails.TimeZoneId);
                return tradingHours;
            }, TimeSpan.FromHours(12)); // Optional TTL of 1 day (adjust as needed)

            // Get the current time in UTC 
            var currentTime = dateTime ?? DateTime.UtcNow;
            // Check if the current time is within the trading hours 
            return tradingHours.Any(th => currentTime >= th.Start && currentTime <= th.End);
        }

        private List<TradingHoursCache> ParseTradingHours(string tradingHoursString, string timeZoneId)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var tradingHours = new List<TradingHoursCache>();

            var daySegments = tradingHoursString.Split(";").Where(day => !day.Contains("CLOSED", StringComparison.InvariantCultureIgnoreCase)).ToList();

            return daySegments.Select(day => {
                var parts = day.Split("-");
                var format = "yyyyMMdd:HHmm";
                var startDayString = parts[0];
                var endDayString = parts[1];
                var startDateTime = DateTime.ParseExact(startDayString, format, null);
                var endDateTime = DateTime.ParseExact(endDayString, format, null);

                return new TradingHoursCache
                {
                    Start = TimeZoneInfo.ConvertTimeToUtc(startDateTime, timeZone),
                    End = TimeZoneInfo.ConvertTimeToUtc(endDateTime, timeZone),
                    Expiration = TimeZoneInfo.ConvertTimeToUtc(endDateTime, timeZone),
                };
            }).ToList();
        }

        private class TradingHoursCache
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public DateTime Expiration { get; set; }
        }
    }
}