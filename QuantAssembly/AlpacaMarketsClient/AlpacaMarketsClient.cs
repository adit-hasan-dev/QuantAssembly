using Alpaca.Markets;
using QuantAssembly.Config;

namespace QuantAssembly.Impl.AlpacaMarkets
{
    public class AlpacaMarketsClientConfig
    {
        public string apiKey { get; set; }
        public string apiSecret { get; set; }
    }
    public class AlpacaMarketsClient
    {
        private readonly string apikey;
        private readonly string apiSecret;
        public AlpacaMarketsClient(IConfig config)
        {
            if (config.CustomProperties.TryGetValue(typeof(AlpacaMarketsClientConfig).Name, out var configObject))
            {
                var customConfig = configObject.ToObject<AlpacaMarketsClientConfig>();
                apikey = customConfig.apiKey;
                apiSecret = customConfig.apiSecret;
            }
            else
            {
                throw new InvalidOperationException($"Configuration for {typeof(AlpacaMarketsClientConfig).Name} not found.");
            }

        }
        public async Task<IEnumerable<IBar>> GetHistoricalDataAsync(string symbol)
        {
            if (string.IsNullOrEmpty(apikey))
            {
                throw new ArgumentNullException(
                    apikey,
                    $"API KEY missing, use `setx ALPACA_KEY \"MY-ALPACA-KEY\"` to set.");
            }

            if (string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentNullException(
                    apiSecret,
                    $"API SECRET missing, use `setx AlpacaApiSecret \"MY-ALPACA-SECRET\"` to set.");
            }

            // connect to Alpaca REST API
            SecretKey secretKey = new(apikey, apiSecret);

            IAlpacaDataClient client = Environments.Live.GetAlpacaDataClient(secretKey);

            DateTime into = DateTime.UtcNow.AddMinutes(-16);
            DateTime from = into.AddYears(-2); // Look-back period of 2 years

            HistoricalBarsRequest request = new(symbol, from, into, BarTimeFrame.Day); // Use daily resolution

            // Fetch daily-bar quotes in Alpaca's format
            IPage<IBar> barSet = await client.ListHistoricalBarsAsync(request);

            return barSet.Items;
        }
    }
}