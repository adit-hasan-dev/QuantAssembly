using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.BackTesting.Models;
using QuantAssembly.BackTesting.Utility;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.DataProvider;
using QuantAssembly.Ledger;

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
            
            var config = baseConfig as Config;
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(GenerateExitSignalsStep)}] Started generating exit signals");
        
            var marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
            var IndicatorDataProvider = serviceProvider.GetRequiredService<IIndicatorDataProvider>();
            var strategyProcessor = context.strategyProcessor;
            foreach (var )
        }

    }
}