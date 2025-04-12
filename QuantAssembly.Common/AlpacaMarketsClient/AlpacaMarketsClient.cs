using Alpaca.Markets;
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
        private readonly SemaphoreSlim rateLimitSemaphore = new(1, 1);
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
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IReadOnlyDictionary<string, T>> GetLatestMarketDataAsync<T>(List<string> symbols)
        {
            var batches = symbols.Distinct().Chunk(config.batchSize);
            List<IReadOnlyDictionary<string, T>> bars = new List<IReadOnlyDictionary<string, T>>();

            foreach (var batch in batches)
            {
                var request = new LatestMarketDataListRequest(batch);
                var result = await ExecuteWithRateLimitHandlingAsync(async () =>
                {
                    return typeof(T) switch
                    {
                        { } when typeof(T) == typeof(IBar) => await client.ListLatestBarsAsync(request) as IReadOnlyDictionary<string, T>,
                        { } when typeof(T) == typeof(IQuote) => await client.ListLatestQuotesAsync(request) as IReadOnlyDictionary<string, T>,
                        _ => null
                    };
                });

                if (result != null)
                {
                    bars.Add(result);
                }
            }

            return bars.SelectMany(x => x).ToDictionary(x => x.Key, x => x.Value);
        }

        public async Task<IOptionContract> GetOptionsContractDetails(string contractSymbol)
        {
            return await ExecuteWithRateLimitHandlingAsync(() =>
                tradingClient.GetOptionContractBySymbolAsync(contractSymbol));
        }

        public async Task<IEnumerable<T>> GetIndicatorDataAsync<T>(
            string symbol,
            DateTime? startTime = null,
            DateTime? endTime = null,
            StepSize? stepSize = null)
        {
            var result = await GetIndicatorDataAsync<T>(new List<string> { symbol }, startTime, endTime, stepSize);
            return result.TryGetValue(symbol, out var bars) ? bars : Enumerable.Empty<T>();
        }

        public async Task<Dictionary<string, IEnumerable<T>>> GetIndicatorDataAsync<T>(
            List<string> symbols,
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
            try
            {
                var batches = symbols.Distinct().Chunk(config.batchSize);
                var tasks = batches.Select(async batch =>
                {
                    var results = new Dictionary<string, IEnumerable<T>>();
                    foreach (var symbol in batch)
                    {
                        var result = await ExecuteWithRateLimitHandlingAsync(async () =>
                        {
                            return typeof(T) switch
                            {
                                { } when typeof(T) == typeof(IBar) => await GetHistoricalBarsAsync(client, symbol, startTime.Value, endTime.Value, stepSize) as IEnumerable<T>,
                                { } when typeof(T) == typeof(IQuote) => await GetHistoricalQuotesAsync(client, symbol, startTime.Value, endTime.Value) as IEnumerable<T>,
                                _ => null
                            };
                        });

                        if (result != null)
                        {
                            results[symbol] = result;
                        }
                    }
                    return results;
                });

                var batchResults = await Task.WhenAll(tasks);
                return batchResults.SelectMany(dict => dict).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            catch (Exception)
            {
                throw;
            }
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
            var response = await ExecuteWithRateLimitHandlingAsync(async () => await optionsDataClient.GetOptionChainAsync(request));
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

                IPage<IBar> barSet = await ExecuteWithRateLimitHandlingAsync(async () => await optionsDataClient.ListHistoricalBarsAsync(request));
                allBars.AddRange(barSet.Items);
                nextPageToken = barSet.NextPageToken;

            } while (nextPageToken != null);

            return allBars;
        }

        public async Task<IClock> GetMarketClockInfoAsync()
        {
            return await ExecuteWithRateLimitHandlingAsync(async () => await tradingClient.GetClockAsync());
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

                IPage<IBar> barSet = await ExecuteWithRateLimitHandlingAsync(async () => await client.ListHistoricalBarsAsync(request));
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

                IPage<IQuote> quoteSet = await ExecuteWithRateLimitHandlingAsync(async () => await client.ListHistoricalQuotesAsync(request));
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

        private async Task<T> ExecuteWithRateLimitHandlingAsync<T>(Func<Task<T>> apiCall)
        {
            while (true)
            {
                try
                {
                    return await apiCall();
                }
                catch (RestClientErrorException ex) when (ex.ErrorCode == 429)
                {
                    await HandleRateLimitAsync();
                }
            }
        }

        private async Task HandleRateLimitAsync()
        {
            await rateLimitSemaphore.WaitAsync();
            try
            {
                // Get rate limit values for all three clients
                var dataClientRateLimit = client.GetRateLimitValues();
                var optionsDataClientRateLimit = optionsDataClient.GetRateLimitValues();
                var tradingClientRateLimit = tradingClient.GetRateLimitValues();

                // Determine the longest wait time among the three clients
                var resetTimes = new[]
                {
            dataClientRateLimit.ResetTimeUtc,
            optionsDataClientRateLimit.ResetTimeUtc,
            tradingClientRateLimit.ResetTimeUtc
        };

                var longestWaitTime = resetTimes.Max() - DateTime.UtcNow;

                if (longestWaitTime > TimeSpan.Zero)
                {
                    await Task.Delay(longestWaitTime);
                }
            }
            finally
            {
                rateLimitSemaphore.Release();
            }
        }
    }
}