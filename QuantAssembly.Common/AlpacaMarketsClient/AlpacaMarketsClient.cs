using Alpaca.Markets;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Constants;

namespace QuantAssembly.Common.Impl.AlpacaMarkets
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

        public async Task<IEnumerable<T>> GetIndicatorDataAsync<T>(
            string symbol,
            DateTime? startTime = null,
            DateTime? endTime = null,
            StepSize? stepSize = null)
        {
            if (string.IsNullOrEmpty(apikey))
            {
                throw new ArgumentNullException(apikey, $"API KEY missing, use `setx ALPACA_KEY \"MY-ALPACA-KEY\"` to set.");
            }

            if (string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentNullException(apiSecret, $"API SECRET missing, use `setx AlpacaApiSecret \"MY-ALPACA-SECRET\"` to set.");
            }

            // Connect to Alpaca REST API
            SecretKey secretKey = new(apikey, apiSecret);
            IAlpacaDataClient client = Environments.Live.GetAlpacaDataClient(secretKey);

            endTime ??= DateTime.UtcNow.AddMinutes(-16);
            startTime ??= endTime.Value.AddYears(-2); // Default look-back period of 2 years

            var result = typeof(T) switch { 
                { } when typeof(T) == typeof(IBar) => await GetHistoricalBarsAsync(client, symbol, startTime.Value, endTime.Value, stepSize) as IEnumerable<T>, 
                { } when typeof(T) == typeof(IQuote) => await GetHistoricalQuotesAsync(client, symbol, startTime.Value, endTime.Value) as IEnumerable<T>, 
                _ => null 
            };

            return result ?? throw new InvalidOperationException($"{typeof(T)} is not supported!");
        }

        private async Task<IEnumerable<IBar>> GetHistoricalBarsAsync(
            IAlpacaDataClient client,
            string symbol,
            DateTime startTime,
            DateTime endTime,
            StepSize? stepSize)
        {
            var barTimeFrame = BarTimeFrame.Day;
            if (stepSize != null)
            {
                barTimeFrame = GetBarTimeFrameFromStepSize(stepSize.Value);
            }

            List<IBar> allBars = new List<IBar>();
            string? nextPageToken = null;

            do
            {
                HistoricalBarsRequest request = new(symbol, startTime, endTime, barTimeFrame)
                {
                    Pagination = {
                        Size = Pagination.MaxPageSize,
                        Token = nextPageToken
                    }
                };

                IPage<IBar> barSet = await client.ListHistoricalBarsAsync(request);
                allBars.AddRange(barSet.Items);
                nextPageToken = barSet.NextPageToken;

            } while (nextPageToken != null);

            return allBars;
        }

        private async Task<IEnumerable<IQuote>> GetHistoricalQuotesAsync(
            IAlpacaDataClient client,
            string symbol,
            DateTime startTime,
            DateTime endTime)
        {
            List<IQuote> allQuotes = new List<IQuote>();
            string? nextPageToken = null;
            do
            {
                HistoricalQuotesRequest request = new(symbol, startTime, endTime)
                {
                    Pagination = {
                        Size = Pagination.MaxPageSize,
                        Token = nextPageToken
                    }
                };

                IPage<IQuote> quoteSet = await client.ListHistoricalQuotesAsync(request);
                allQuotes.AddRange(quoteSet.Items);
                nextPageToken = quoteSet.NextPageToken;

            } while (nextPageToken != null);

            return allQuotes;
        }

        private static BarTimeFrame GetBarTimeFrameFromStepSize(StepSize stepSize)
        {
            return stepSize switch
            {
                StepSize.OneMinute => BarTimeFrame.Minute,
                StepSize.ThirtyMinutes => new BarTimeFrame(30, BarTimeFrameUnit.Minute),
                StepSize.OneHour => BarTimeFrame.Hour,
                StepSize.OneDay => BarTimeFrame.Day,
                StepSize.OneWeek => BarTimeFrame.Week,
                StepSize.OneMonth => BarTimeFrame.Month,
                _ => BarTimeFrame.Day
            };
        }

    }
}