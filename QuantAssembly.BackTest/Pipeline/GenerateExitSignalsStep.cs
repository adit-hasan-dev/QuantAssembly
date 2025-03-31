using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.BackTesting.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.Models;
using QuantAssembly.DataProvider;

namespace QuantAssembly.BackTesting
{
    [PipelineStep]
    [PipelineStepInput(nameof(BacktestContext.openPositions))]
    [PipelineStepOutput(nameof(QuantContext.signals))]
    public class GenerateExitSignalsStep : IPipelineStep<BacktestContext>
    {
        public async Task Execute(BacktestContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
        {
            if (context.strategyProcessor == null)
            {
                throw new PipelineException($"[{nameof(GenerateExitSignalsStep)}] StrategyProcessor is not initialized in the context");
            }
            
            var config = baseConfig as Models.Config;
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(GenerateExitSignalsStep)}] Started generating exit signals for {context.openPositions.Count} open positions");
        
            var marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
            var IndicatorDataProvider = serviceProvider.GetRequiredService<IIndicatorDataProvider>();
            var strategyProcessor = context.strategyProcessor;
            var exitSignals = new List<Signal>();
            foreach (var position in context.openPositions)
            {
                var marketData = await marketDataProvider.GetMarketDataAsync(position.Symbol);
                var indicatorData = await IndicatorDataProvider.GetIndicatorDataAsync(position.Symbol);
                position.CurrentPrice = marketData.LatestPrice;
                var exitSignal = strategyProcessor.EvaluateCloseSignal(marketData, indicatorData, position);

                if (exitSignal == SignalType.Exit && exitSignal == SignalType.StopLoss)
                {
                    logger.LogInfo($"[{nameof(GenerateExitSignalsStep)}] Exit conditions met for position: {position}, signalType: {exitSignal}");
                    exitSignals.Add(new Signal
                    {
                        Type = exitSignal,
                        SymbolName = position.Symbol,
                        PositionGuid = position.PositionGuid,
                        MarketData = marketData,
                        IndicatorData = indicatorData
                    });
                    // TODO: Updatebacktest report
                }
                logger.LogInfo($"[{nameof(GenerateExitSignalsStep)}] Generated {exitSignals.Count} exit signals for {context.openPositions.Count} open positions");
                context.signals.AddRange(exitSignals);
            }
        }

    }
}