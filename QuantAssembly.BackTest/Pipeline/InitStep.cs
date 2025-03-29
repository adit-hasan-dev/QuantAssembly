using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.BackTesting.Models;
using QuantAssembly.BackTesting.Utility;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.DataProvider;
using QuantAssembly.Ledger;
using QuantAssembly.RiskManagement;
using QuantAssembly.Strategy;

namespace QuantAssembly.BackTesting
{
    [PipelineStep]
    [PipelineStepOutput(nameof(BacktestContext.openPositions))]
    [PipelineStepOutput(nameof(BacktestContext.symbolsToEvaluate))]
    [PipelineStepOutput(nameof(BacktestContext.accountData))]
    public class InitStep : IPipelineStep<BacktestContext>
    {
        public async Task Execute(BacktestContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
        {
            var config = baseConfig as Config;
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(InitStep)}] Started generating exit signals");
            
            var ledger = serviceProvider.GetRequiredService<ILedger>();
            var timeMachine = serviceProvider.GetRequiredService<TimeMachine>();
            var marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
            var accountDataProvider = serviceProvider.GetRequiredService<IAccountDataProvider>();
            var openPositions = ledger.GetOpenPositions();
            marketDataProvider!.FlushMarketDataCache();
            context.accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
            
            var filteredPositions = new List<Position>();
            foreach (var position in openPositions)
            {
                if (await marketDataProvider.IsWithinTradingHours(position.Symbol, timeMachine.GetCurrentTime()))
                {
                    filteredPositions.Add(position);
                }
                else
                {
                    logger.LogDebug($"[{nameof(InitStep)}] Not processing exit signals for symbol: {position.Symbol} since it is outside its trading hours");
                }
            }
            filteredPositions = filteredPositions.Where(position => {
                var strategy = context.strategyProcessor!.GetStrategy(position.Symbol);

                if (strategy.State == StrategyState.Halted)
                {
                    logger.LogInfo($"[{nameof(InitStep)}] Strategy {strategy.Name} for symbol {position.Symbol} is halted. Not processing any exit signals");
                    return false;
                }
                return true;
            }).ToList();

            context.openPositions = filteredPositions;

            // Filter out invalid tickers for entry signals
            // Skip entry signals if risk manager says so
            var riskManager = serviceProvider.GetRequiredService<IRiskManager>();
            if (riskManager.shouldHaltNewTrades(context.accountData))
            {
                logger.LogInfo($"[{nameof(InitStep)}] OPENING NEW POSITIONS HALTED! PLEASE CHECK PORTFOLIO STATE.");
                return;
            }

            foreach (var symbol in config.TickerStrategyMap.Keys)
            {
                if (await marketDataProvider.IsWithinTradingHours(symbol, DateTime.UtcNow))
                {
                    context.symbolsToEvaluate.Add(symbol);
                }
                else
                {
                    logger.LogInfo($"[{nameof(InitStep)}] Not processing entry signals for symbol: {symbol} since it is outside its trading hours");
                }
            }

            context.symbolsToEvaluate = context.symbolsToEvaluate.Where(symbol => {
                var strategy = context.strategyProcessor!.GetStrategy(symbol);
                if (strategy.State != StrategyState.Active)
                {
                    logger.LogInfo($"[{nameof(InitStep)}] Strategy {strategy.Name} for symbol {symbol} is inactive. Not processing any entry signals");
                    return false;
                }

                return true;
            }).ToHashSet();

            logger.LogInfo($"[{nameof(InitStep)}] Successfully completed init step");
        }

    }
}