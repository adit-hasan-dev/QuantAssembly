using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.Models;
using QuantAssembly.Core.Strategy;

namespace QuantAssembly.Core.Pipeline
{
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.openPositions))]
    [PipelineStepOutput(nameof(QuantContext.signals))]
    public class GenerateExitSignalsStep<TContext> : IPipelineStep<TContext> where TContext : QuantContext, new()
    {
        public async Task Execute(TContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
        {
            var strategyProcessor = serviceProvider.GetRequiredService<IStrategyProcessor>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            if (context.openPositions == null || !context.openPositions.Any())
            {
                logger.LogInfo($"[{nameof(GenerateExitSignalsStep<TContext>)}] No open positions so skipping evaluating for exit signals");
                return;
            }
            if (strategyProcessor == null)
            {
                throw new PipelineException($"[{nameof(GenerateExitSignalsStep<TContext>)}] StrategyProcessor is not initialized in the context");
            }
            
            var config = baseConfig as Models.Config;
            logger.LogInfo($"[{nameof(GenerateExitSignalsStep<TContext>)}] Started generating exit signals for {context.openPositions.Count} open positions");
        
            var marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
            var IndicatorDataProvider = serviceProvider.GetRequiredService<IIndicatorDataProvider>();
            var exitSignals = new List<Signal>();
            foreach (var position in context.openPositions)
            {
                var marketData = await marketDataProvider.GetMarketDataAsync(position.Symbol);
                var indicatorData = await IndicatorDataProvider.GetIndicatorDataAsync(position.Symbol);
                position.CurrentPrice = marketData.LatestPrice;
                var exitSignal = strategyProcessor.EvaluateCloseSignal(marketData, indicatorData, position);

                if (
                    exitSignal == SignalType.Exit ||
                    exitSignal == SignalType.StopLoss ||
                    exitSignal == SignalType.TakeProfit)
                {
                    logger.LogInfo($"[{nameof(GenerateExitSignalsStep<TContext>)}] Exit conditions met for position: {position}, signalType: {exitSignal}");
                    exitSignals.Add(new Signal
                    {
                        Type = exitSignal,
                        SymbolName = position.Symbol,
                        PositionGuid = position.PositionGuid,
                        MarketData = marketData,
                        IndicatorData = indicatorData
                    });
                }
            }
            logger.LogInfo($"[{nameof(GenerateExitSignalsStep<TContext>)}] Generated {exitSignals.Count} exit signals for {context.openPositions.Count} open positions");
            context.signals.AddRange(exitSignals);
        }

    }
}