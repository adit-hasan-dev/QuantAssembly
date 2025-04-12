using QuantAssembly.Common.Models;
using QuantAssembly.Core.Models;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Core.Strategy;
using QuantAssembly.Common.Ledger;

namespace QuantAssembly.Core.Pipeline
{

    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.symbolsToEvaluate))]
    [PipelineStepOutput(nameof(QuantContext.signals))]
    public class GenerateEntrySignalsStep<TContext> : IPipelineStep<TContext> where TContext : QuantContext, new()
    {
        public async Task Execute(TContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var strategyProcessor = serviceProvider.GetRequiredService<IStrategyProcessor>();
            var logger = serviceProvider.GetService<ILogger>();
            if (strategyProcessor == null)
            {
                throw new PipelineException($"[{nameof(GenerateEntrySignalsStep<TContext>)}] StrategyProcessor is not initialized in the context");
            }
            if (!context.symbolsToEvaluate?.Any() ?? false)
            {
                logger.LogInfo($"[{nameof(GenerateEntrySignalsStep<TContext>)}] There are no symbols to evaluate and generate entry signals for");
                return;
            }
            logger.LogInfo($"[{nameof(GenerateEntrySignalsStep<TContext>)}] Started generating entry signals");

            var marketDataProvider = serviceProvider.GetService<IMarketDataProvider>();
            var IndicatorDataProvider = serviceProvider.GetRequiredService<IIndicatorDataProvider>();
            var ledger = serviceProvider.GetService<ILedger>();

            var entrySignals = new List<Signal>();

            foreach (var symbol in context.symbolsToEvaluate)
            {
                var marketData = await marketDataProvider.GetMarketDataAsync(symbol);
                var histData = await IndicatorDataProvider.GetIndicatorDataAsync(symbol);
                var signalType = strategyProcessor.EvaluateOpenSignal(marketData, context.accountData, histData, symbol);
                if (signalType == SignalType.Entry)
                {
                    logger.LogInfo($"[{nameof(GenerateEntrySignalsStep<TContext>)}] Entry conditions for symbol: {symbol} met");
                    entrySignals.Add(new Signal
                    {
                        SymbolName = symbol,
                        Type = signalType,
                        MarketData = marketData,
                        IndicatorData = histData
                    });
                }
            }
            context.signals.AddRange(entrySignals);
            logger.LogInfo($"[{nameof(GenerateEntrySignalsStep<TContext>)}] Successfully generated {entrySignals.Count} entry signals");
        }
    }
}