using System.Reflection.Metadata;
using Alpaca.Markets;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Models;
using QuantAssembly.RiskManagement;

namespace QuantAssembly
{
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.signals))]
    [PipelineStepOutput(nameof(QuantContext.positionsToOpen))]
    public class RiskManagementStep : IPipelineStep<QuantContext>
    {
        public async Task Execute(QuantContext context, ServiceProvider serviceProvider)
        {
            var riskManager = serviceProvider.GetRequiredService<IRiskManager>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var positions = context.signals.Where(
                signal => signal.Type == SignalType.Entry)
                .Select(signal => {
                    var position = PrepareOpenPosition(context, signal.SymbolName, signal.MarketData);
                    if (!riskManager.ComputePositionSize(signal.MarketData, signal.HistoricalMarketData, context.accountData, position))
                    {
                        logger.LogInfo($"[{nameof(RiskManagementStep)}] Appropriate resources not available to open position:\n {position}");
                    }

                    return position;
                })
                .Where(position => position.Quantity > 0)
                .ToList();
            context.positionsToOpen.AddRange(positions);
        }

        private Position PrepareOpenPosition(QuantContext context, string symbol, MarketData marketData)
        {
            var strategy = context.strategyProcessor.GetStrategy(symbol);
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