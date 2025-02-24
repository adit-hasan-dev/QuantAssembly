using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.DataProvider;
using QuantAssembly.Ledger;
using QuantAssembly.Models;

namespace QuantAssembly
{
    /// <summary>
    /// This class generates a list of entry signals from the symbols to evaluate
    /// </summary>
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.symbolsToEvaluate))]
    [PipelineStepOutput(nameof(QuantContext.signals))]
    public class GenerateEntrySignalsStep : IPipelineStep<QuantContext>
    {
        public async Task Execute(QuantContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            ValidatePrerequisites(context);
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
                var signalType = context.strategyProcessor.EvaluateOpenSignal(marketData, context.accountData, histData, symbol);
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

        private void ValidatePrerequisites(QuantContext context)
        {
            if (context.strategyProcessor == null)
            {
                throw new PipelineException($"[{nameof(GenerateEntrySignalsStep)}] StrategyProcessor is not initialized in the context");
            }
            if (context.symbolsToEvaluate?.Any() ?? false)
            {
                throw new PipelineException($"[{nameof(GenerateEntrySignalsStep)}] No symbols to evaluate. Double check the configuration");
            }
        }
    }
}