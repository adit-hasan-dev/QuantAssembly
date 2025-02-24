using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.DataProvider;
using QuantAssembly.Ledger;
using QuantAssembly.RiskManagement;
using QuantAssembly.Strategy;

namespace QuantAssembly
{
    /// <summary>
    /// Responsible for setting up the initial state of the pipeline
    /// </summary>
    [PipelineStep]
    [PipelineStepOutput(nameof(QuantContext.openPositions))]
    [PipelineStepOutput(nameof(QuantContext.symbolsToEvaluate))]
    [PipelineStepOutput(nameof(QuantContext.accountData))]
    public class InitStep : IPipelineStep<QuantContext>
    {
        public async Task Execute(QuantContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
        {
            var config = baseConfig as Config;
            var ledger = serviceProvider.GetService<ILedger>();
            var logger = serviceProvider.GetService<ILogger>();
            logger.LogInfo($"[{nameof(InitStep)}] Started init step");
            
            var marketDataProvider = serviceProvider.GetService<IMarketDataProvider>();
            var accountDataProvider = serviceProvider.GetService<IAccountDataProvider>();
            var openPositions = ledger!.GetOpenPositions();
            marketDataProvider!.FlushMarketDataCache();
            context.accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
            var filteredPositions = new List<Position>();

            // Filter out invalid positions for exit signals
            foreach (var position in openPositions)
            {
                if (await marketDataProvider.IsWithinTradingHours(position.Symbol, DateTime.UtcNow))
                {
                    filteredPositions.Add(position);
                }
                else
                {
                    logger.LogInfo($"[{nameof(InitStep)}] Not processing exit signals for symbol: {position.Symbol} since it is outside its trading hours");
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