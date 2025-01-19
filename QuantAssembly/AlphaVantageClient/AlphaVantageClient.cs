using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.RateLimit;
using Polly.Wrap;
using QuantAssembly.Common.Logging;
using Polly.Retry;

namespace QuantAssembly.Impl.AlphaVantage
{
    public class AlphaVantageClient
    {
        private readonly HttpClient httpClient;
        private readonly ILogger logger;
        private readonly string apiKey;
        private readonly AsyncRetryPolicy waitPolicy;
        private static readonly string baseUrl = "https://www.alphavantage.co/query";
        private static readonly int retryCount = 3;
        private static readonly int backOffTimeInSeconds = 5;
        private static readonly int numCallsPerMin = 5;
        private readonly string cacheDirectory;

        public AlphaVantageClient(ILogger logger, string apiKey)
        {
            httpClient = new HttpClient();
            this.logger = logger;
            this.apiKey = apiKey;
            cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(backOffTimeInSeconds));

            var rateLimitPolicy = Policy
                .RateLimitAsync(numCallsPerMin, TimeSpan.FromMinutes(1));

            waitPolicy = Policy
                .Handle<RateLimitRejectedException>()
                .WaitAndRetryAsync(1, sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(60), onRetry: (exception, timespan) =>
                {
                    logger.LogInfo($"Rate limit exceeded. Retrying in {timespan.TotalSeconds} seconds.");
                });

            waitPolicy.WrapAsync(rateLimitPolicy).WrapAsync(retryPolicy);
        }

        private string GetCacheFilePath(string ticker, string indicator)
        {
            var path = Path.Combine(cacheDirectory, $"{ticker}_{indicator}_{DateTime.UtcNow:yyyy-MM-dd}.json");
            logger.LogDebug($"[AlphaVantageClient] Cached file path for ticker: {ticker}, indicator: {indicator} => {path}");
            return path;
        }

        private async Task<T> GetCachedDataAsync<T>(string ticker, string indicator, Func<Task<T>> fetchData)
        {
            var cacheFilePath = GetCacheFilePath(ticker, indicator);

            if (File.Exists(cacheFilePath))
            {
                logger.LogDebug($"[AlphaVantageClient] Reading from cache ticker: {ticker}, indicator: {indicator}");
                var jsonData = await File.ReadAllTextAsync(cacheFilePath);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonData) ?? throw new InvalidDataException("Failed to deserialize cached property");
            }
            else
            {
                logger.LogDebug($"[AlphaVantageClient] Fetching data for ticker: {ticker}, indicator: {indicator}");
                var data = await fetchData();
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                await File.WriteAllTextAsync(cacheFilePath, jsonData);
                return data;
            }
        }

        /// <summary>
        /// Gets the latest RSI value for a given ticker.
        /// Time Interval: 14 days
        /// Resolution: Daily
        /// </summary>
        public async Task<double> GetRSIAsync(string ticker)
        {
            return await GetCachedDataAsync(ticker, "RSI", async () =>
            {
                var url = $"{baseUrl}?function=RSI&symbol={ticker}&interval=daily&time_period=14&series_type=close&apikey={apiKey}";

                var response = await waitPolicy.ExecuteAsync(() => httpClient.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var jsonData = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonData);
                var latestRSI = (double?)json?["Technical Analysis: RSI"]?.First?.First?["RSI"] ?? throw new InvalidDataException("RSI missing");

                return latestRSI;
            });
        }

        /// <summary>
        /// Gets the latest SMA value for a given ticker over a specified time period.
        /// Time Interval: 50 days and 200 days
        /// Resolution: Daily
        /// </summary>
        public async Task<double> GetSMAAsync(string ticker, int timePeriod)
        {
            return await GetCachedDataAsync(ticker, $"SMA_{timePeriod}", async () =>
            {
                var url = $"{baseUrl}?function=SMA&symbol={ticker}&interval=daily&time_period={timePeriod}&series_type=close&apikey={apiKey}";

                var response = await waitPolicy.ExecuteAsync(() => httpClient.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var jsonData = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonData);
                var latestSMA = (double?)json?["Technical Analysis: SMA"]?.First?.First["SMA"] ?? throw new InvalidDataException("Technical Analysis: SMA");

                return latestSMA;
            });
        }

        /// <summary>
        /// Gets the latest EMA value for a given ticker over a specified time period.
        /// Time Interval: 50 days
        /// Resolution: Daily
        /// </summary>
        public async Task<double> GetEMAAsync(string ticker, int timePeriod)
        {
            return await GetCachedDataAsync(ticker, $"EMA_{timePeriod}", async () =>
            {
                var url = $"{baseUrl}?function=TIME_SERIES_DAILY&symbol={ticker}&outputsize=compact&apikey={apiKey}";

                var response = await waitPolicy.ExecuteAsync(() => httpClient.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var jsonData = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonData);
                var timeSeries = (JObject?)json["Time Series (Daily)"] ?? throw new InvalidDataException("Time Series (Daily) missing.");

                var closePrices = new List<double>();
                foreach (var item in timeSeries)
                {
                    var data = (JObject?)item.Value;
                    closePrices.Add((double?)data?["4. close"] ?? throw new InvalidDataException("close price missing"));
                }

                return CalculateEMA(closePrices, timePeriod);
            });
        }

        private double CalculateEMA(List<double> values, int period)
        {
            double k = 2.0 / (period + 1);
            double ema = values[0];

            for (int i = 1; i < values.Count; i++)
            {
                ema = (values[i] * k) + (ema * (1 - k));
            }

            return ema;
        }


        // TODO: MACD is a premium endpoint, need to compute manually
        /// <summary>
        /// Gets the latest MACD value and signal line for a given ticker.
        /// Time Interval: 12-day EMA (short-term), 26-day EMA (long-term), and 9-day EMA (signal line)
        /// Resolution: Daily
        /// </summary>
        public async Task<(double MACD, double Signal)> GetMACDAsync(string ticker)
        {
            var historicalData = await FetchHistoricalDataAsync(ticker);

            var closePrices = historicalData.Values.ToList();
            List<double> macdValues = new List<double>();

            // Calculate historical MACD values
            for (int i = 1; i <= closePrices.Count; i++)
            {
                double shortTermEMA = CalculateEMA(closePrices.Take(i).ToList(), 12);
                double longTermEMA = CalculateEMA(closePrices.Take(i).ToList(), 26);
                macdValues.Add(shortTermEMA - longTermEMA);
            }

            // Current MACD line
            double macdLine = macdValues.Last();

            // Calculate the signal line as the 9-day EMA of the MACD values
            double signalLine = CalculateEMA(macdValues, 9);

            return (macdLine, signalLine);
        }

        /// <summary>
        /// Gets the latest Bollinger Bands values for a given ticker.
        /// Time Interval: 20 days
        /// Resolution: Daily
        /// </summary>
        public async Task<(double UpperBand, double LowerBand)> GetBollingerBandsAsync(string ticker)
        {
            return await GetCachedDataAsync(ticker, "BollingerBands", async () =>
            {
                var url = $"{baseUrl}?function=BBANDS&symbol={ticker}&interval=daily&time_period=20&series_type=close&apikey={apiKey}";

                var response = await waitPolicy.ExecuteAsync(() => httpClient.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var jsonData = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonData);
                var latestUpperBand = (double?)json?["Technical Analysis: BBANDS"]?.First?.First?["Real Upper Band"] ?? throw new InvalidDataException("Real Upper Band missing");
                var latestLowerBand = (double?)json?["Technical Analysis: BBANDS"]?.First?.First?["Real Lower Band"] ?? throw new InvalidDataException("Real Lower Band missing");

                return (latestUpperBand, latestLowerBand);
            });
        }

        /// <summary>
        /// Gets the latest ATR value for a given ticker.
        /// Time Interval: 14 days
        /// Resolution: Daily
        /// </summary>
        public async Task<double> GetATRAsync(string ticker)
        {
            return await GetCachedDataAsync(ticker, "ATR", async () =>
            {
                var url = $"{baseUrl}?function=ATR&symbol={ticker}&interval=daily&time_period=14&apikey={apiKey}";

                var response = await waitPolicy.ExecuteAsync(() => httpClient.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var jsonData = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonData);
                var latestATR = (double?)json?["Technical Analysis: ATR"]?.First?.First?["ATR"] ?? throw new InvalidDataException("Technical Analysis: ATR missing");

                return latestATR;
            });
        }

        /// <summary>
        /// Gets the historical high and low prices for a given ticker from the last 200 days.
        /// Time Interval: 200 days
        /// Resolution: Daily
        /// </summary>
        public async Task<(double HistoricalHigh, double HistoricalLow)> GetHistoricalHighLowAsync(string ticker)
        {
            var historicalData = await FetchHistoricalDataAsync(ticker);

            var highPrices = historicalData.Values.ToList();
            var lowPrices = historicalData.Values.ToList();

            var historicalHigh = highPrices.Max();
            var historicalLow = lowPrices.Min();

            return (historicalHigh, historicalLow);
        }

        private async Task<Dictionary<string, double>> FetchHistoricalDataAsync(string ticker)
        {
            return await GetCachedDataAsync(ticker, "HistoricalData", async () =>
            {
                var url = $"{baseUrl}?function=TIME_SERIES_DAILY&symbol={ticker}&outputsize=compact&apikey={apiKey}";

                var response = await waitPolicy.ExecuteAsync(() => httpClient.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var jsonData = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonData);
                var timeSeries = (JObject?)json["Time Series (Daily)"] ?? throw new InvalidDataException("Time Series (Daily) missing.");

                var historicalData = new Dictionary<string, double>();
                foreach (var item in timeSeries)
                {
                    var data = (JObject?)item.Value;
                    historicalData.Add(item.Key, (double?)data?["4. close"] ?? throw new InvalidDataException("close price missing"));
                }

                return historicalData;
            });
        }


    }

}

