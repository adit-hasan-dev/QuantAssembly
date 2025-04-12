using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.Models;
using QuantAssembly.Core.TradeManager;
using QuantAssembly.Common.Constants;

namespace QuantAssembly.Core.Pipeline
{
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.signals))]
    [PipelineStepInput(nameof(QuantContext.openPositions))]
    [PipelineStepOutput(nameof(QuantContext.transactions))]
    [PipelineStepOutput(nameof(QuantContext.accountData))]
    public class ClosePositionsStep<TContext> : IPipelineStep<TContext> where TContext : QuantContext, new()
    {
        public async Task Execute(TContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
        {
            // We don't invoke the RiskManager to close positions we just sell 
            // the entire position. So this can be done before the RiskManagement step
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var exitSignals = context.signals.Where(
                signal => signal.Type == SignalType.Exit ||
                signal.Type == SignalType.StopLoss ||
                signal.Type == SignalType.TakeProfit);
            var tradeManager = serviceProvider.GetRequiredService<ITradeManager>();
            var accountDataProvider = serviceProvider.GetRequiredService<IAccountDataProvider>();

            if (exitSignals == null || !exitSignals.Any())
            {
                logger.LogInfo($"[{nameof(ClosePositionsStep<TContext>)}] No exit signals to process");
                return;
            }

            foreach (var signal in exitSignals)
            {
                var position = context.openPositions.FirstOrDefault(position => position.PositionGuid == signal.PositionGuid);
                
                if (position == null)
                {
                    throw new PipelineException($"[{nameof(ClosePositionsStep<TContext>)}] Position not found for signal: {signal}");
                }
                // TODO: Make OrderType configurable
                var result = await tradeManager.ClosePositionAsync(position, OrderType.Market);
                if (result?.TransactionState != TransactionState.Completed)
                {
                    logger.LogWarn($"[{nameof(ClosePositionsStep<TContext>)}] Failed to close position: {position}");
                }
                position.CloseReason = signal.Type switch
                {
                    SignalType.Exit => ClosePositionReason.ExitConditionHit,
                    SignalType.StopLoss => ClosePositionReason.StopLossLevelHit,
                    SignalType.TakeProfit => ClosePositionReason.TakeProfitLevelHit,
                    _ => position.CloseReason
                };
                context.transactions.Add(result);
            }
            var config = baseConfig as Config;
            context.accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
        }
    }
}