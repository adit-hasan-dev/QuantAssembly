
using System.Threading.Tasks;
using QuantAssembly.Common.Impl.AlpacaMarkets;

namespace QuantAssembly.Common
{
    public class RealTimeProvider : ITimeProvider
    {
        private readonly AlpacaMarketsClient alpacaMarketsClient;
        public RealTimeProvider(AlpacaMarketsClient alpacaMarketsClient)
        {
            this.alpacaMarketsClient = alpacaMarketsClient;
        }
        public async Task<DateTime> GetCurrentTime()
        {
            var clock = await alpacaMarketsClient.GetMarketClockInfoAsync();
            return clock.TimestampUtc;
        }

        public async Task<bool> IsWithinTradingHoursAsync()
        {
            var clock = await alpacaMarketsClient.GetMarketClockInfoAsync();
            return clock.IsOpen;
        }
    }
}