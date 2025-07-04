using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Config;
using QuantAssembly.Core.TradeManager;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.Models;

namespace QuantAssembly
{
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.signals))]
    [PipelineStepInput(nameof(QuantContext.openPositions))]
    [PipelineStepOutput(nameof(QuantContext.transactions))]
    [PipelineStepOutput(nameof(QuantContext.accountData))]
    public class ClosePositionsStep : IPipelineStep<QuantContext>
    {
        public async Task Execute(QuantContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
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
            var config = baseConfig as Config;
            context.accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
        }
    }

}