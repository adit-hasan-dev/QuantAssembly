using System.Collections.Concurrent;
using Alpaca.Markets;
using QuantAssembly.Common;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Common.Instrumentation;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.DataProvider;
using Skender.Stock.Indicators;

namespace QuantAssembly.BackTesting.DataProvider
{
    public class BacktestMarketDataProvider : IIndicatorDataProvider, IMarketDataProvider
    {
        private readonly TimeMachine timeMachine;
        private readonly ITimeProvider timeProvider;
        private readonly Dictionary<string, List<IBar>> historicalDataCache = new();
        private readonly Dictionary<string, MarketData> marketDataCache = new();
        private readonly Dictionary<string, IList<TimestampedIndicatorData>> historicalIndicatorDataCache = new();
        private readonly AlpacaMarketsClient alpacaClient;
        private readonly List<string> tickers;
        private bool isInitialized = false;
        private readonly ILogger logger;
        private readonly string cacheFolderPath;
        private readonly int precomputeTaskBatchSize = 100;

        private class TimestampedIndicatorData
        {
            public DateTime Timestamp { get; set; }
            public IndicatorData Data { get; set; }
        }

        public BacktestMarketDataProvider(
            TimeMachine timeMachine,
            ITimeProvider timeProvider,
            AlpacaMarketsClient alpacaClient,
            ILogger logger,
            string cacheFolderPath,
            List<string> tickers)
        {
            this.timeMachine = timeMachine;
            this.alpacaClient = alpacaClient;
            this.tickers = tickers;
            this.logger = logger;
            this.cacheFolderPath = cacheFolderPath;
            this.timeProvider = timeProvider;
        }

        public async Task<IndicatorData> GetIndicatorDataAsync(string ticker)
        {
            if (!isInitialized)
            {
                await Initialize();
            }
            var currentSimTime = timeMachine.GetCurrentTime();
            // Find the data for the currentSimTime 
            var historicalData = historicalIndicatorDataCache[ticker];
            var interim = historicalData.Where(data => data.Timestamp <= currentSimTime);
            interim = interim.OrderBy(data => data.Timestamp);
            var result = interim.LastOrDefault();

            return result?.Data;
        }

        public async Task<MarketData> GetMarketDataAsync(string symbol)
        {
            if (!isInitialized)
            {
                await Initialize();
            }
            var currentSimTime = timeMachine.GetCurrentTime();
            var marketData = marketDataCache[symbol];

            var latestBar = historicalDataCache[symbol]
                .Where(bar => bar.TimeUtc <= currentSimTime)
                .OrderByDescending(bar => bar.TimeUtc)
                .FirstOrDefault();

            if (latestBar != null)
            {
                marketData.LatestPrice = (double)latestBar.Open;
            }

            return marketData;
        }

        public void FlushMarketDataCache()
        {
            // no-op
            return;
        }

        private async Task Initialize()
        {
            try
            {
                await LatencyLogger.DoWithLatencyLoggerAsync(async () =>
                {
                    foreach (var ticker in tickers)
                    {
                        logger.LogInfo($"[BacktestMarketDataProvider::Inititalize] Getting historical price data for ticker: {ticker}, time period: {timeMachine.timePeriod} and step size: {timeMachine.stepSize}");
                        var historicalDataStartTime = timeMachine.startTime.Subtract(TimeSpan.FromDays(365));
                        var historicalData = await this.alpacaClient.GetIndicatorDataAsync<IBar>(ticker, historicalDataStartTime, timeMachine.endTime, StepSize.ThirtyMinutes);
                        historicalDataCache[ticker] = historicalData.ToList();
                        marketDataCache[ticker] = new MarketData
                        {
                            Symbol = ticker,
                            LatestPrice = (double)historicalData.First().Close,
                            AskPrice = (double)historicalData.First().High,
                            BidPrice = (double)historicalData.First().Low
                        };
                        logger.LogInfo($"[BacktestMarketDataProvider::Inititalize] Precomputing historical indicator data for ticker: {ticker}, time period: {timeMachine.timePeriod} and step size: {timeMachine.stepSize}");

                        await LatencyLogger.DoWithLatencyLoggerAsync(async () =>
                        {
                            var progress = new Progress<int>(completedTasks =>
                            {
                                logger.LogInfo($"Completed {completedTasks} precompute tasks for ticker: {ticker}");
                            });
                            // Precompute playout the entire time interval, so we give it 
                            // a separate instance that doesn't interfere with the global time machine
                            var newTimeMachine = timeMachine.Clone();
                            await PreComputeHistoricalIndicators(ticker, newTimeMachine, progress);
                        }, "BacktestMarketDataProvider::PreComputeHistoricalIndicators", logger);
                    }
                    isInitialized = true;
                }, "BacktestMarketDataProvider::Initialize", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw;
            }
        }

        private async Task PreComputeHistoricalIndicators(string ticker, TimeMachine timeMachine, IProgress<int> progress)
        {
            var cacheFilePath = $"{cacheFolderPath}/{ticker}_{timeMachine.timePeriod}_{timeMachine.stepSize}_HistoricalIndicatorsCache.json";
            var cachedData = await CacheHelper.LoadFromCacheAsync<List<TimestampedIndicatorData>>(cacheFilePath);
            var dataList = new ConcurrentBag<TimestampedIndicatorData>(cachedData ?? new List<TimestampedIndicatorData>());

            DateTime lastComputedTime = DateTime.MinValue;
            if (cachedData != null && cachedData.Count > 0)
            {
                lastComputedTime = cachedData.Max(data => data.Timestamp);
                logger.LogInfo($"Resuming pre-computation from {lastComputedTime} for {ticker} with {timeMachine.timePeriod} and {timeMachine.stepSize}");
            }

            // Check if the cached data is complete using lastComputedTime
            if (cachedData != null && lastComputedTime >= timeMachine.endTime)
            {
                logger.LogInfo($"Cache hit for {ticker} with {timeMachine.timePeriod} and {timeMachine.stepSize}. Using cached data.");
                historicalIndicatorDataCache[ticker] = cachedData;
                return;
            }

            logger.LogInfo($"Starting pre-computation for {ticker} with {timeMachine.timePeriod} and {timeMachine.stepSize}");
            var tasks = new List<Task>();
            var completedTaskCount = dataList.Count;
            var semaphore = new SemaphoreSlim(precomputeTaskBatchSize);

            while (timeMachine.GetCurrentTime() < timeMachine.endTime)
            {
                var currentTime = timeMachine.GetCurrentTime();
                if (currentTime > lastComputedTime && await timeProvider.IsWithinTradingHoursAsync())
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var historicalData = historicalDataCache[ticker].Where(bar => bar.TimeUtc <= currentTime).ToList();
                            var indicatorData = ComputeIndicators(ticker, historicalData);
                            dataList.Add(new TimestampedIndicatorData
                            {
                                Timestamp = currentTime,
                                Data = indicatorData
                            });
                            Interlocked.Increment(ref completedTaskCount);
                            if (completedTaskCount % precomputeTaskBatchSize == 0)
                            {
                                progress.Report(completedTaskCount);
                                // Save progress to cache file
                                var partialDataList = dataList.OrderBy(data => data.Timestamp).ToList();
                                await CacheHelper.SaveToCacheAsync(cacheFilePath, partialDataList);
                                logger.LogDebug($"Progress saved to cache for {ticker} with {timeMachine.timePeriod} and {timeMachine.stepSize}. Completed: {completedTaskCount} tasks.");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex);
                            throw;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
                timeMachine.StepForward();
            }

            await Task.WhenAll(tasks);
            var historicalDataList = dataList.OrderBy(data => data.Timestamp).ToList();
            historicalIndicatorDataCache[ticker] = historicalDataList;

            logger.LogDebug($"Completed pre-computation for {ticker} with {timeMachine.timePeriod} and {timeMachine.stepSize}");

            // Save to cache
            await CacheHelper.SaveToCacheAsync(cacheFilePath, historicalDataList);
            logger.LogDebug($"Cache saved for {ticker} with {timeMachine.timePeriod} and {timeMachine.stepSize}");
        }

        private IndicatorData ComputeIndicators(string ticker, IList<IBar> historicalData)
        {
            var quotes = historicalData.Select(bar => new Quote
            {
                Date = bar.TimeUtc,
                Open = bar.Open,
                High = bar.High,
                Low = bar.Low,
                Close = bar.Close,
                Volume = bar.Volume
            }).OrderBy(x => x.Date).ToList();

            var rsi = quotes.GetRsi(14).LastOrDefault()?.Rsi ?? 0;
            var sma50 = quotes.GetSma(50).LastOrDefault()?.Sma ?? 0;
            var sma200 = quotes.GetSma(200).LastOrDefault()?.Sma ?? 0;
            var ema50 = quotes.GetEma(50).LastOrDefault()?.Ema ?? 0;
            var macdResult = quotes.GetMacd(12, 26, 9).LastOrDefault();
            var macd = macdResult?.Macd ?? 0;
            var signal = macdResult?.Signal ?? 0;
            var bollingerBands = quotes.GetBollingerBands(20).LastOrDefault();
            var upperBand = bollingerBands?.UpperBand ?? 0;
            var lowerBand = bollingerBands?.LowerBand ?? 0;
            var atr = quotes.GetAtr(14).LastOrDefault()?.Atr ?? 0;
            var historicalHigh = quotes.Max(q => q.High);
            var historicalLow = quotes.Min(q => q.Low);

            var data = new IndicatorData
            {
                Symbol = ticker,
                RSI = rsi,
                SMA_50 = sma50,
                SMA_200 = sma200,
                EMA_50 = ema50,
                MACD = macd,
                Signal = signal,
                Upper_Band = upperBand,
                Lower_Band = lowerBand,
                ATR = atr,
                HistoricalHigh = (double)historicalHigh,
                HistoricalLow = (double)historicalLow
            };

            return data;
        }
    }

}