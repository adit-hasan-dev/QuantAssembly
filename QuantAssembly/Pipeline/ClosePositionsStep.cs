using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.TradeManager;
using QuantAssembly.Models;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Logging;
using QuantAssembly.DataProvider;
using QuantAssembly.Common.Config;

namespace QuantAssembly
{
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.signals))]
    [PipelineStepInput(nameof(QuantContext.openPositions))]
    [PipelineStepOutput(nameof(QuantContext.transactions))]
    [PipelineStepOutput(nameof(QuantContext.accountData))]
    public class ClosePositionsStep : IPipelineStep<QuantContext>
    {
        public async Task Execute(QuantContext context, ServiceProvider serviceProvider)
        {
            // We don't invoke the RiskManager to close positions we just sell 
            // the entire position. So this can be done before the RiskManagement step
            var tradeManager = serviceProvider.GetRequiredService<ITradeManager>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var accountDataProvider = serviceProvider.GetRequiredService<IAccountDataProvider>();
            var exitSignals = context.signals.Where(
                signal => signal.Type == SignalType.Exit ||
                signal.Type == SignalType.StopLoss ||
                signal.Type == SignalType.TakeProfit);

            foreach (var signal in exitSignals)
            {
                var position = context.openPositions.FirstOrDefault(position => position.PositionGuid == signal.PositionGuid);
                
                if (position == null)
                {
                    throw new PipelineException($"[{nameof(ClosePositionsStep)}] Position not found for signal: {signal}");
                }
                // TODO: Make OrderType configurable
                var result = await tradeManager.ClosePositionAsync(position, OrderType.Market);
                if (result?.TransactionState != TransactionState.Completed)
                {
                    logger.LogWarn($"[{nameof(ClosePositionsStep)}] Failed to close position: {position}");
                }
                context.transactions.Add(result);
            }
            var config = serviceProvider.GetRequiredService<IConfig>();
            context.accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
        }
    }

}