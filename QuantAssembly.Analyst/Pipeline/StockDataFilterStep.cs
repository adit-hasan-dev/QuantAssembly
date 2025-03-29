using Alpaca.Markets;
using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Analyst.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    /// <summary>
    /// Populates the market data for the companies
    /// Filters out based on retrieved data
    /// </summary>
    [PipelineStep]
    [PipelineStepInput(nameof(AnalystContext.companies))]
    [PipelineStepOutput(nameof(AnalystContext.marketData))]
    public class StockDataFilterStep : IPipelineStep<AnalystContext>
    {
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(StockDataFilterStep)}] Retrieving real-time data for {context.companies.Count} symbols");

            var alpacaClient = serviceProvider.GetRequiredService<AlpacaMarketsClient>();
            var symbols = context.companies.Select(c => c.Symbol).ToList();

            // Perform all Alpaca client calls asynchronously
            var latestBarsTask = alpacaClient.GetLatestMarketDataAsync<IBar>(symbols);
            var latestQuotesTask = alpacaClient.GetLatestMarketDataAsync<IQuote>(symbols);
            var weeklyBarsTask = alpacaClient.GetIndicatorDataAsync<IBar>(
                symbols,
                startTime: DateTime.UtcNow.AddDays(-7).AddMinutes(-16), // Adjust as needed
                endTime: DateTime.UtcNow.AddMinutes(-16),
                stepSize: StepSize.OneWeek
            );

            Task.WhenAll(latestBarsTask, latestQuotesTask, weeklyBarsTask).Wait();
            var latestBars = await latestBarsTask;
            var latestQuotes = await latestQuotesTask;
            var weeklyBars = await weeklyBarsTask;

            // Construct market data using the results of all three calls
            var marketData = latestBars.Keys
                .Intersect(latestQuotes.Keys)
                .Intersect(weeklyBars.Keys)
                .ToDictionary(
                    symbol => symbol,
                    symbol => (latestBars[symbol], latestQuotes[symbol], weeklyBars[symbol].FirstOrDefault())
                )
                .Select(kv => new AnalystMarketData
                {
                    Symbol = kv.Key,
                    LatestPrice = (double)kv.Value.Item2.AskPrice,
                    AskPrice = (double)kv.Value.Item2.AskPrice,
                    BidPrice = (double)kv.Value.Item2.BidPrice,
                    Open = (double)kv.Value.Item1.Open,
                    Close = (double)kv.Value.Item1.Close,
                    High = (double)kv.Value.Item1.High,
                    Low = (double)kv.Value.Item1.Low,
                    Volume = kv.Value.Item3 != null ? (double)kv.Value.Item3.Volume / 5 : 0, // Weekly average volume divided by 5
                    Vwap = (double)kv.Value.Item2.AskPrice,
                    TradeCount = (double)kv.Value.Item2.BidPrice
                }).ToList();

            // Filter based on market data
            var marketDataFilterConfig = (config as Config).marketDataFilterConfig;
            var filteredData = marketData.AsQueryable();

            if (marketDataFilterConfig.MinPrice.HasValue)
                filteredData = filteredData.Where(md => md.LatestPrice >= marketDataFilterConfig.MinPrice.Value);

            if (marketDataFilterConfig.MaxPrice.HasValue)
                filteredData = filteredData.Where(md => md.LatestPrice <= marketDataFilterConfig.MaxPrice.Value);

            if (marketDataFilterConfig.MinVolume.HasValue)
                filteredData = filteredData.Where(md => md.Volume >= marketDataFilterConfig.MinVolume.Value);

            if (marketDataFilterConfig.MaxSpreadPercentage.HasValue)
                filteredData = filteredData.Where(md =>
                    ((md.AskPrice - md.BidPrice) / md.BidPrice) * 100 <= marketDataFilterConfig.MaxSpreadPercentage.Value);

            if (marketDataFilterConfig.MaxChangePercentage.HasValue)
                filteredData = filteredData.Where(md =>
                    Math.Abs((md.LatestPrice - md.Open) / md.Open) * 100 <= marketDataFilterConfig.MaxChangePercentage.Value);

            context.marketData = filteredData.ToList();
            logger.LogInfo($"[{nameof(StockDataFilterStep)}] Filtration complete. {context.marketData.Count} symbols remaining.");
        }
    }
}

