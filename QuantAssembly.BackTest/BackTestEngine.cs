using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.BackTesting.DataProvider;
using QuantAssembly.BackTesting.TradeManager;
using QuantAssembly.BackTesting.Utility;
using QuantAssembly.BackTesting.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.DataProvider;
using QuantAssembly.Impl.AlpacaMarkets;
using QuantAssembly.Ledger;
using QuantAssembly.Common.Logging;
using QuantAssembly.Models;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Models;
using QuantAssembly.RiskManagement;
using QuantAssembly.Strategy;
using QuantAssembly.TradeManager;
using QuantAssembly.Utility;

namespace QuantAssembly.BackTesting
{
    public class BacktestConfig
    {
        public double InitialPortfolioValue { get; set; }

    }
    public class BackTestEngine
    {
        private ServiceProvider serviceProvider;
        private TimePeriod timePeriod;
        private StepSize stepSize;
        private IConfig config;
        private ILogger logger;
        private IStrategyProcessor strategyProcessor;
        private IMarketDataProvider marketDataProvider;
        private IHistoricalMarketDataProvider historicalMarketDataProvider;
        private IRiskManager riskManager;
        private IAccountDataProvider accountDataProvider;
        private ITradeManager tradeManager;

        private BacktestConfig backtestConfig;
        private BackTestSummary backtestSummary = new();

        public BackTestEngine(TimePeriod timePeriod, StepSize stepSize)
        {
            this.timePeriod = timePeriod;
            this.stepSize = stepSize;

            Initialize();
            logger = serviceProvider.GetRequiredService<ILogger>();
            config = serviceProvider.GetRequiredService<IConfig>();
            logger.LogInfo("Initializing BacktestEngine...");
            // Load all strategies

            logger.LogInfo($"[BacktestEngine] Loading all strategies.");
            this.strategyProcessor = new StrategyProcessor(logger);
            foreach (var (ticker, strategyPath) in config.TickerStrategyMap)
            {
                try
                {
                    this.strategyProcessor.LoadStrategyFromFile(ticker, strategyPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }

            this.accountDataProvider = serviceProvider.GetRequiredService<IAccountDataProvider>();
            this.marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
            this.historicalMarketDataProvider = serviceProvider.GetRequiredService<IHistoricalMarketDataProvider>();
            this.riskManager = new PercentageAccountValueRiskManager(serviceProvider);
            this.tradeManager = serviceProvider.GetRequiredService<ITradeManager>();
            logger.LogInfo("Successfully initialized BackTestEngine.");
        }

        public async Task Run()
        {
            logger.LogInfo($"[BackTestEngine::Run] Starting main loop with polling interval: {this.stepSize} and time period: {this.timePeriod}");
            var timeMachine = serviceProvider.GetRequiredService<TimeMachine>();
            var ledger = serviceProvider.GetRequiredService<ILedger>();
            backtestSummary.InitialPortfolioValue = (await this.accountDataProvider.GetAccountDataAsync(config.AccountId)).TotalPortfolioValue;
            while (timeMachine.GetCurrentTime() < timeMachine.endTime)
            {
                await ProcessSignals(ledger, timeMachine, this.accountDataProvider);
                timeMachine.StepForward();
            }
            backtestSummary.FinalPortfolioValue = (await this.accountDataProvider.GetAccountDataAsync(config.AccountId)).TotalPortfolioValue;

            logger.LogInfo($"[BackTestEngine::Run] Completed backtesting period");
            logger.LogInfo(this.backtestSummary.ToString());
        }

        private async Task ProcessSignals(ILedger ledger, TimeMachine timeMachine, IAccountDataProvider accountDataProvider)
        {
            logger.LogDebug($"[BacktestEngine::ProcessSignals] Processing Signals for time: {timeMachine.GetCurrentTime()}");
            var openPositions = ledger.GetOpenPositions();
            var accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
            var filteredPositions = new List<Position>();
            foreach (var position in openPositions)
            {
                if (await marketDataProvider.IsWithinTradingHours(position.Symbol, timeMachine.GetCurrentTime()))
                {
                    filteredPositions.Add(position);
                }
                else
                {
                    logger.LogDebug($"[BackTestEngine::ProcessSignals] Not processing exit signals for symbol: {position.Symbol} since it is outside its trading hours");
                }
            }

            // Process exit signals for all open positions
            foreach (var position in filteredPositions)
            {
                Validator.AssertPropertiesNonNull(
                    position,
                    new List<string>{
                        "PositionGuid",
                        "Symbol",
                        "OpenTime",
                        "Currency",
                        "OpenPrice",
                        "CurrentPrice",
                        "Quantity",
                        "StrategyName",
                        "StrategyDefinition"
                    });
                var strategy = this.strategyProcessor.GetStrategy(position.Symbol);
                if (strategy.State != StrategyState.Halted)
                {
                    var marketData = await marketDataProvider.GetMarketDataAsync(position.Symbol);
                    var histData = await historicalMarketDataProvider.GetHistoricalDataAsync(position.Symbol);
                    await ProcessExitSignal(position, marketData, histData);
                }
                else
                {
                    logger.LogError($"[BackTestEngine::ProcessSignals] Strategy {strategy.Name} for symbol {position.Symbol} is halted. Not processing any exit signals");
                }
            }

            if (this.riskManager.shouldHaltNewTrades(accountData))
            {
                logger.LogInfo($"[BackTestEngine::ProcesSignals] OPENING NEW POSITIONS HALTED! PLEASE CHECK PORTFOLIO STATE.");
                return;
            }

            List<Position> positionsOpened = new List<Position>();
            foreach (var symbol in config.TickerStrategyMap.Keys)
            {
                bool isWithinTradingHours = await marketDataProvider.IsWithinTradingHours(symbol, timeMachine.GetCurrentTime());
                if (!isWithinTradingHours)
                {
                    logger.LogDebug($"[BackTestEngine::ProcessSignals] Not processing entry signals for symbol: {symbol} since it is outside its trading hours.");
                    continue;
                }
                var strategy = this.strategyProcessor.GetStrategy(symbol);

                if (strategy.State == StrategyState.Active)
                {
                    MarketData marketData = null;
                    HistoricalMarketData histData = null;

                    marketData = await marketDataProvider.GetMarketDataAsync(symbol);
                    histData = await historicalMarketDataProvider.GetHistoricalDataAsync(symbol);
                    await ProcessEntrySignal(symbol, positionsOpened, marketData, histData);
                }
                else
                {
                    logger.LogError($"[BackTestEngine::ProcessSignals] Strategy {strategy.Name} for symbol {symbol} is halted. Not processing any entry signals");
                }
            }
        }

        private async Task ProcessExitSignal(Position position, MarketData marketData, HistoricalMarketData histData)
        {
            logger.LogDebug($"[BacktestEngine::ProcessExitSignal] Processing exit signals for {position.Symbol}");
            var accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);

            // Update current price according to latest market data for position
            position.CurrentPrice = marketData.LatestPrice;
            var exitSignal = strategyProcessor.EvaluateCloseSignal(marketData, histData, position);
            if (exitSignal != SignalType.None)
            {
                logger.LogInfo($"[BacktestEngine::ProcessExitSignal] Exit conditions met for position: {position}, signalType: {exitSignal}");
                UpdateBacktestReport(exitSignal, position.StrategyName);
                // For now, we don't engage the risk manager to close positions
                // We just sell off the entire position
                // TODO: Hard-coding in the order type as a market sell, need to support others
                var result = await tradeManager.ClosePositionAsync(position, OrderType.Market);
                // TODO: Handle unsuccessful transactions
                if (result.TransactionState == TransactionState.Completed)
                {
                    Validator.AssertPropertiesNonNull(
                    position,
                    new List<string>{
                        "PositionGuid",
                        "Symbol",
                        "OpenTime",
                        "CloseTime",
                        "Currency",
                        "OpenPrice",
                        "ClosePrice",
                        "CurrentPrice",
                        "Quantity",
                        "ProfitOrLoss",
                        "StrategyName",
                        "StrategyDefinition"
                    });
                }
                else
                {
                    logger.LogInfo($"[BacktestEngine::ProcessExitSignal] Failed to close position: {position}");
                }
            }
            else
            {
                logger.LogDebug($"[BacktestEngine::ProcessExitSignal] Exit conditions weren't met for position: {position}");
            }

            logger.LogDebug($"[BacktestEngine::ProcessExitSignal] Successfully processed exit signals for position: {position}");
        }

        private async Task ProcessEntrySignal(string symbol, IList<Position> positionsOpened, MarketData marketData, HistoricalMarketData histData)
        {
            logger.LogDebug($"[BacktestEngine::ProcessEntrySignal] Processing Entry Signals for {symbol}");
            var accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
            var openSignal = strategyProcessor.EvaluateOpenSignal(marketData, accountData, histData, symbol);
            if (openSignal != SignalType.None)
            {
                logger.LogInfo($"[BacktestEngine::ProcessEntrySignal] Entry conditions met for symbol: {symbol}");
                var position = PrepareOpenPosition(symbol, marketData);
                UpdateBacktestReport(openSignal, position.StrategyName);
                if (riskManager.ComputePositionSize(marketData, histData, accountData, position))
                {
                    // TODO: Current only using market buys. Need to support other types like limit buys
                    var result = await tradeManager.OpenPositionAsync(position, OrderType.Market);
                    if (result.TransactionState == TransactionState.Completed)
                    {
                        Validator.AssertPropertiesNonNull(
                            position,
                            new List<string>{
                                "PositionGuid",
                                "Symbol",
                                "State",
                                "OpenTime",
                                "Currency",
                                "OpenPrice",
                                "CurrentPrice",
                                "Quantity",
                                "StrategyName",
                                "StrategyDefinition"
                            });
                        positionsOpened.Add(position);
                        logger.LogInfo($"[BacktestEngine::ProcessEntrySignal] Successfully opened position: {position}");
                    }
                    else
                    {
                        logger.LogError($"[BacktestEngine::ProcessEntrySignal] Failed to open position:\n{position}");
                    }
                }
                else
                {
                    logger.LogDebug($"[BacktestEngine::ProcessEntrySignal] Appropriate resources not available to open position:\n {position}");
                }
            }
            else
            {
                logger.LogDebug($"[BacktestEngine::ProcessEntrySignal] Entry conditions weren't met for symbol {symbol}");
            }

            logger.LogDebug($"[BacktestEngine::ProcessEntrySignal] Successfully processed entry signals for symbol: {symbol}");
        }

        private Position PrepareOpenPosition(string symbol, MarketData marketData)
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

        private void UpdateBacktestReport(SignalType signalType, string strategyName)
        {
            var report = this.backtestSummary.backtestStrategyReports.FirstOrDefault(r => r.StrategyName.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
            if (report == null) 
            { 
                report = new BacktestStrategyReport { StrategyName = strategyName }; 
                this.backtestSummary.backtestStrategyReports.Add(report); 
            }
            switch (signalType)
            {
                case SignalType.Exit:
                    report.ExitConditionsHit++;
                    break;
                case SignalType.StopLoss:
                    report.StopLossConditionsHit++;
                    break;
                case SignalType.TakeProfit:
                    report.TakeProfitConditionsHit++;
                    break;
                case SignalType.Entry:
                    report.EntryConditionsHit++;
                    break;
            }
        }

        private void Initialize()
        {
            var services = new ServiceCollection();
            serviceProvider = services
            .AddSingleton<IConfig, Config>()
            .AddSingleton<ILogger, Logger>(provider =>
            {
                var config = provider.GetRequiredService<IConfig>();
                return new Logger(config, isDevEnv: false);
            })
            .AddSingleton<ILedger, Ledger.Ledger>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                var config = provider.GetRequiredService<IConfig>();
                return new Ledger.Ledger(config, logger);
            })
            .AddSingleton<AlpacaMarketsClient>(provider =>
            {
                var config = provider.GetRequiredService<IConfig>();
                return new AlpacaMarketsClient(config);
            })
            .AddSingleton<TimeMachine>(provider =>
            {
                var startTime = DateTime.UtcNow.Subtract(Common.Utility.GetTimeSpanFromTimePeriod(timePeriod)).Subtract(TimeSpan.FromMinutes(16));
                return new TimeMachine(this.timePeriod, this.stepSize, startTime);
            })
            .AddSingleton<BacktestMarketDataProvider>(provider =>
            {
                var timeMachine = provider.GetRequiredService<TimeMachine>();
                var alpacaClient = provider.GetRequiredService<AlpacaMarketsClient>();
                var config = provider.GetRequiredService<IConfig>();
                return new BacktestMarketDataProvider(timeMachine, alpacaClient, logger, config);
            })
            .AddSingleton<IHistoricalMarketDataProvider, BacktestMarketDataProvider>(provider =>
            {
                return provider.GetRequiredService<BacktestMarketDataProvider>();
            })
            .AddSingleton<IMarketDataProvider, BacktestMarketDataProvider>(provider =>
            {
                return provider.GetRequiredService<BacktestMarketDataProvider>();
            })
            .AddSingleton<IAccountDataProvider, BacktestAccountDataProvider>(provider =>
            {
                var config = provider.GetRequiredService<IConfig>();
                var customProperties = config.CustomProperties;
                if (customProperties.TryGetValue(typeof(BacktestConfig).Name, out var configObject))
                {
                    this.backtestConfig = configObject?.ToObject<BacktestConfig>() ?? new();
                }
                else
                {
                    throw new InvalidOperationException($"Configuration for {typeof(BacktestConfig).Name} not found.");
                }
                return new BacktestAccountDataProvider(config.AccountId, this.backtestConfig.InitialPortfolioValue);
            })
            .AddSingleton<ITradeManager, BacktestTradeManager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                var ledger = provider.GetRequiredService<ILedger>();
                var accountDataProvider = provider.GetRequiredService<IAccountDataProvider>() as BacktestAccountDataProvider;
                var timeMachine = provider.GetRequiredService<TimeMachine>();
                return new BacktestTradeManager(ledger, logger,accountDataProvider, timeMachine, config.AccountId);
            })
            .BuildServiceProvider();
        }
    }
}