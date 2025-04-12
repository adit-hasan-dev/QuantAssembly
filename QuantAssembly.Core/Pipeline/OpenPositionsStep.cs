using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.Models;
using QuantAssembly.Core.TradeManager;

namespace QuantAssembly.Core.Pipeline
{
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.positionsToOpen))]
    [PipelineStepOutput(nameof(QuantContext.transactions))]
    [PipelineStepOutput(nameof(QuantContext.accountData))]
    public class OpenPositionsStep<TContext> : IPipelineStep<TContext> where TContext : QuantContext, new()
    {
        public async Task Execute(TContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            // We expect the RiskManager to have already computed the position size
            var logger = serviceProvider.GetRequiredService<ILogger>();
            if (context.positionsToOpen == null || !context.positionsToOpen.Any())
            {
                logger.LogInfo($"[{nameof(OpenPositionsStep<TContext>)}] No positions to open");
                return;
            }
            var tradeManager = serviceProvider.GetRequiredService<ITradeManager>();
            var accountDataProvider = serviceProvider.GetRequiredService<IAccountDataProvider>();
            foreach(var position in context.positionsToOpen)
            {
                logger.LogInfo($"[{nameof(OpenPositionsStep<TContext>)}] Attempting to open position: {position}");
                var result = await tradeManager.OpenPositionAsync(position, OrderType.Market);

                if (result?.TransactionState != TransactionState.Completed)
                {
                    logger.LogWarn($"[{nameof(OpenPositionsStep<TContext>)}] Failed to open position: {position}");
                }
                context.transactions.Add(result);
            }
            var quantConfig = config as Config;
            context.accountData = await accountDataProvider.GetAccountDataAsync(quantConfig.AccountId);
        }
    }
}