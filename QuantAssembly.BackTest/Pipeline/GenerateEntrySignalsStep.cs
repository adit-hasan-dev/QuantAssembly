using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.BackTesting.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Ledger;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.Models;
using QuantAssembly.Core.Strategy;
using QuantAssembly.DataProvider;

namespace QuantAssembly.BackTesting
{
    [PipelineStep]
    [PipelineStepInput(nameof(BacktestContext.symbolsToEvaluate))]
    [PipelineStepOutput(nameof(BacktestContext.signals))]
    public class GenerateEntrySignalsStep : IPipelineStep<BacktestContext>
    {
        public async Task Execute(BacktestContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var strategyProcessor = serviceProvider.GetRequiredService<IStrategyProcessor>();
            if (strategyProcessor == null)
            {
                throw new PipelineException($"[{nameof(GenerateEntrySignalsStep)}] StrategyProcessor is not initialized in the context");
            }
            if (!context.symbolsToEvaluate?.Any() ?? false)
            {
                return;
            }
            var logger = serviceProvider.GetService<ILogger>();
            logger.LogInfo($"[{nameof(GenerateEntrySignalsStep)}] Started generating entry signals");

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
                    logger.LogInfo($"[{nameof(GenerateEntrySignalsStep)}] Entry conditions for symbol: {symbol} met");
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
            logger.LogInfo($"[{nameof(GenerateEntrySignalsStep)}] Successfully generated {entrySignals.Count} entry signals");
        }
    }
}