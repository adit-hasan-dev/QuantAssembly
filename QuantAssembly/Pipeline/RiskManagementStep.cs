using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Core.Models;
using QuantAssembly.Core.RiskManagement;
using QuantAssembly.Core.Strategy;

namespace QuantAssembly
{
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.signals))]
    [PipelineStepOutput(nameof(QuantContext.positionsToOpen))]
    public class RiskManagementStep : IPipelineStep<QuantContext>
    {
        public async Task Execute(QuantContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
        {
            var riskManager = serviceProvider.GetRequiredService<IRiskManager>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var strategyProcessor = serviceProvider.GetRequiredService<IStrategyProcessor>();
            var positions = context.signals.Where(
                signal => signal.Type == SignalType.Entry)
                .Select(signal => {
                    var position = PrepareOpenPosition(strategyProcessor, signal.SymbolName, signal.MarketData);
                    if (!riskManager.ComputePositionSize(signal.MarketData, signal.IndicatorData, context.accountData, position))
                    {
                        logger.LogInfo($"[{nameof(RiskManagementStep)}] Appropriate resources not available to open position:\n {position}");
                    }

                    return position;
                })
                .Where(position => position.Quantity > 0)
                .ToList();
            context.positionsToOpen.AddRange(positions);
        }

        private Position PrepareOpenPosition(IStrategyProcessor strategyProcessor, string symbol, MarketData marketData)
        {
            var strategy = strategyProcessor.GetStrategy(symbol);
            var position = new Position
            {
                PositionGuid = Guid.NewGuid(),
                Symbol = symbol,
                State = PositionState.Open,
                StrategyName = strategy.Name,
                StrategyDefinition = JsonConvert.SerializeObject(strategy),
                CurrentPrice = marketData.LatestPrice
            };
            return position;
        }
    }
}