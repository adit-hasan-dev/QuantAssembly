using Alpaca.Markets;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Constants;

namespace QuantAssembly.Common.Impl.AlpacaMarkets
{
    public class AlpacaMarketsClientConfig
    {
        public string apiKey { get; set; }
        public string apiSecret { get; set; }
        public int batchSize { get; set; }
    }
    public class AlpacaMarketsClient
    {
        private readonly IAlpacaDataClient client;
        private readonly IAlpacaOptionsDataClient optionsDataClient;
        private readonly IAlpacaTradingClient tradingClient;
        private readonly AlpacaMarketsClientConfig config;
        public AlpacaMarketsClient(AlpacaMarketsClientConfig config)
        {
            try 
            {
                this.config = config;
                // Connect to Alpaca REST API
                SecretKey secretKey = new(config.apiKey, config.apiSecret);
                client = Environments.Live.GetAlpacaDataClient(secretKey);
                optionsDataClient = Environments.Paper.GetAlpacaOptionsDataClient(secretKey);
                tradingClient = Environments.Paper.GetAlpacaTradingClient(secretKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Configuration for {typeof(AlpacaMarketsClientConfig).Name} not found.");
            }
        }

        public async Task<IReadOnlyDictionary<string, T>> GetLatestMarketDataAsync<T>(List<string> symbols)
        {
            var batches = symbols.Distinct().Chunk(config.batchSize);
            List<IReadOnlyDictionary<string, T>> bars = new List<IReadOnlyDictionary<string, T>>();
            foreach (var batch in batches)
            {
                var request = new LatestMarketDataListRequest(batch);
                var result = typeof(T) switch 
                {
                    { } when typeof(T) == typeof(IBar) => await client.ListLatestBarsAsync(request) as IReadOnlyDictionary<string, T>,
                    { } when typeof(T) == typeof(IQuote) => await client.ListLatestQuotesAsync(request) as IReadOnlyDictionary<string, T>,
                    _ => null
                };
                bars.Add(result);
            }
            return bars.SelectMany(x => x).ToDictionary(x => x.Key, x => x.Value);
        }

        public async Task<IOptionContract> GetOptionsContractDetails(string contractSymbol)
        {
            return await tradingClient.GetOptionContractBySymbolAsync(contractSymbol);
        }

        public async Task<IEnumerable<T>> GetIndicatorDataAsync<T>(
            string symbol,
            DateTime? startTime = null,
            DateTime? endTime = null,
            StepSize? stepSize = null)
        {
            if (string.IsNullOrEmpty(this.config.apiKey))
            {
                throw new ArgumentNullException(this.config.apiKey, $"API KEY missing, use `setx ALPACA_KEY \"MY-ALPACA-KEY\"` to set.");
            }

            if (string.IsNullOrEmpty(this.config.apiSecret))
            {
                throw new ArgumentNullException(this.config.apiSecret, $"API SECRET missing, use `setx AlpacaApiSecret \"MY-ALPACA-SECRET\"` to set.");
            }

            endTime ??= DateTime.UtcNow.AddMinutes(-16);
            startTime ??= endTime.Value.AddYears(-2); // Default look-back period of 2 years

            var result = typeof(T) switch { 
                { } when typeof(T) == typeof(IBar) => await GetHistoricalBarsAsync(client, symbol, startTime.Value, endTime.Value, stepSize) as IEnumerable<T>, 
                { } when typeof(T) == typeof(IQuote) => await GetHistoricalQuotesAsync(client, symbol, startTime.Value, endTime.Value) as IEnumerable<T>, 
                _ => null 
            };

            return result ?? throw new InvalidOperationException($"{typeof(T)} is not supported!");
        }

        public async Task<IEnumerable<IOptionSnapshot>> GetOptionsChainDataAsync(
            string symbol,
            DateOnly? earliestExpirationDate)
        {
            if (string.IsNullOrEmpty(this.config.apiKey))
            {
                throw new ArgumentNullException(this.config.apiKey, $"API KEY missing, use `setx ALPACA_KEY \"MY-ALPACA-KEY\"` to set.");
            }

            if (string.IsNullOrEmpty(this.config.apiSecret))
            {
                throw new ArgumentNullException(this.config.apiSecret, $"API SECRET missing, use `setx AlpacaApiSecret \"MY-ALPACA-SECRET\"` to set.");
            }

            OptionChainRequest request = new OptionChainRequest(symbol)
            {
                ExpirationDateGreaterThanOrEqualTo = earliestExpirationDate ?? DateOnly.FromDateTime(DateTime.UtcNow)
            };
            var response = await optionsDataClient.GetOptionChainAsync(request);
            return response.Items.Values;
        }

        public async Task<IEnumerable<IBar>> GetOptionHistoricalBarsAsync(
            string symbol)
        {
            List<IBar> allBars = new List<IBar>();
            string? nextPageToken = null;

            do
            {
                HistoricalOptionBarsRequest request = new(symbol, DateTime.UtcNow.AddDays(-9), DateTime.UtcNow.AddDays(-3), BarTimeFrame.Day)
                {
                    Pagination = {
                        Size = Pagination.MaxPageSize,
                        Token = nextPageToken
                    }
                };

                IPage<IBar> barSet = await optionsDataClient.ListHistoricalBarsAsync(request);
                allBars.AddRange(barSet.Items);
                nextPageToken = barSet.NextPageToken;

            } while (nextPageToken != null);

            return allBars;
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