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
    [PipelineStepInput(nameof(QuantContext.positionsToOpen))]
    [PipelineStepOutput(nameof(QuantContext.transactions))]
    [PipelineStepOutput(nameof(QuantContext.accountData))]
    public class OpenPositionsStep : IPipelineStep<QuantContext>
    {
        public async Task Execute(QuantContext context, ServiceProvider serviceProvider)
        {
            // We expect the RiskManager to have already computed the position size
            var tradeManager = serviceProvider.GetRequiredService<ITradeManager>();
            var accountDataProvider = serviceProvider.GetRequiredService<IAccountDataProvider>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
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
            var config = serviceProvider.GetRequiredService<IConfig>();
            context.accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
        }
    }

}