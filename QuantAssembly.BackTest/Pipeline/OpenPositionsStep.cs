using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.BackTesting.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.Models;
using QuantAssembly.Core.TradeManager;

namespace QuantAssembly.BackTesting
{
    [PipelineStep]
    [PipelineStepInput(nameof(BacktestContext.positionsToOpen))]
    [PipelineStepOutput(nameof(BacktestContext.transactions))]
    [PipelineStepOutput(nameof(BacktestContext.accountData))]
    public class OpenPositionsStep : IPipelineStep<BacktestContext>
    {
        public async Task Execute(BacktestContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            // We expect the RiskManager to have already computed the position size
            var logger = serviceProvider.GetRequiredService<ILogger>();
            if (context.positionsToOpen == null || !context.positionsToOpen.Any())
            {
                logger.LogInfo($"[{nameof(OpenPositionsStep)}] No positions to open");
                return;
            }
            var tradeManager = serviceProvider.GetRequiredService<ITradeManager>();
            var accountDataProvider = serviceProvider.GetRequiredService<IAccountDataProvider>();
            foreach(var position in context.positionsToOpen)
            {
                logger.LogInfo($"[{nameof(OpenPositionsStep)}] Attempting to open position: {position}");
                var result = await tradeManager.OpenPositionAsync(position, OrderType.Market);

                if (result?.TransactionState != TransactionState.Completed)
                {
                    logger.LogWarn($"[{nameof(OpenPositionsStep)}] Failed to open position: {position}");
                }
                context.transactions.Add(result);
            }
            var quantConfig = config as Config;
            context.accountData = await accountDataProvider.GetAccountDataAsync(quantConfig.AccountId);
        }
    }
}