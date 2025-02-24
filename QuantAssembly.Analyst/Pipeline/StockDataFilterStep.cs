using Alpaca.Markets;
using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Analyst.Models;
using QuantAssembly.Common.Config;
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
            logger.LogInfo($"[{nameof(StockDataFilterStep)}] Retrieving real time data for {context.companies.Count} symbols");
            
            var alpacaClient = serviceProvider.GetRequiredService<AlpacaMarketsClient>();
            var symbols = context.companies.Select(c => c.Symbol).ToList();
            var latestBars = await alpacaClient.GetLatestMarketDataAsync<IBar>(symbols);
            var latestQuotes = await alpacaClient.GetLatestMarketDataAsync<IQuote>(symbols);
            var marketDataFilterConfig = (config as Models.Config).marketDataFilterConfig;
            
            var marketData = latestBars.Keys
                .Intersect(latestQuotes.Keys)
                .ToDictionary(
                    symbol => symbol,
                    symbol => (latestBars[symbol], latestQuotes[symbol])
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
                    Volume = (double)kv.Value.Item1.Volume,
                    Vwap = (double)kv.Value.Item2.AskPrice,
                    TradeCount = (double)kv.Value.Item2.BidPrice

                }).ToList();

                // Filter based on market data
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

