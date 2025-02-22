using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.DataProvider;
using QuantAssembly.Models;

namespace QuantAssembly
{
    
    /// <summary>
    /// This class generates a list of exit signals from the open positions
    /// </summary>
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.openPositions))]
    [PipelineStepOutput(nameof(QuantContext.signals))]
    public class GenerateExitSignalsStep : IPipelineStep<QuantContext>
    {
        public async Task Execute(QuantContext context, ServiceProvider serviceProvider)
        {
            ValidatePrerequisites(context);

            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(GenerateExitSignalsStep)}] Started generating exit signals");
            
            var marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
            var historicalMarketDataProvider = serviceProvider.GetRequiredService<IHistoricalMarketDataProvider>();
            var config = serviceProvider.GetRequiredService<IConfig>();
            
            List<Signal> exitSignals = new List<Signal>();

            foreach (var position in context.openPositions)
            {
                var marketData = await marketDataProvider.GetMarketDataAsync(position.Symbol);
                var historicalData = await historicalMarketDataProvider.GetHistoricalDataAsync(position.Symbol);
                position.CurrentPrice = marketData.LatestPrice;

                var signalType = context!.strategyProcessor!.EvaluateCloseSignal(marketData, historicalData, position);

                if (signalType == SignalType.Exit || signalType == SignalType.StopLoss || signalType == SignalType.TakeProfit)
                {
                    logger.LogInfo($"[{nameof(GenerateExitSignalsStep)}] Entry conditions for symbol: {position.Symbol} met, SignalType: {signalType}");
                    exitSignals.Add(new Signal
                    {
                        SymbolName = position.Symbol,
                        Type = signalType,
                        MarketData = marketData,
                        HistoricalMarketData = historicalData,
                        PositionGuid = position.PositionGuid
                    });
                }
            }

            context.signals.AddRange(exitSignals);
            logger.LogInfo($"[{nameof(GenerateExitSignalsStep)}] Successfully generated {exitSignals.Count} exit signals");
        }

        private void ValidatePrerequisites(QuantContext context)
        {
            if (context.strategyProcessor == null)
            {
                throw new PipelineException($"[{nameof(GenerateExitSignalsStep)}] StrategyProcessor is not initialized in the context");
            }
        }
    }
}