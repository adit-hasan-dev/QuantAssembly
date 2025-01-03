using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.Config;
using QuantAssembly.DataProvider;
using QuantAssembly.Impl.AlpacaMarkets;
using QuantAssembly.Impl.AlphaVantage;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Ledger;
using QuantAssembly.Logging;
using QuantAssembly.Models;
using QuantAssembly.Models.Constants;
using QuantAssembly.RiskManagement;
using QuantAssembly.Strategy;
using QuantAssembly.TradeManager;
using Validator = QuantAssembly.Utility.Validator;

namespace QuantAssembly
{
    public class Quant
    {
        private IConfig config;
        private ILogger logger;

        private ServiceProvider serviceProvider;
        private IAccountDataProvider accountDataProvider;
        private IMarketDataProvider marketDataProvider;
        private IHistoricalMarketDataProvider historicalMarketDataProvider;

        private IStrategyProcessor strategyProcessor;
        private IRiskManager riskManager;
        private ITradeManager tradeManager;

        private int pollingIntervalInMs;
        private bool shouldTerminate;
        private AccountData accountData;

        public Quant()
        {
            Inititalize();
            logger = serviceProvider.GetRequiredService<ILogger>();
            config = serviceProvider.GetRequiredService<IConfig>();
            logger.LogInfo("Initializing Quant...");

            // Load all strategies
            logger.LogInfo($"[Quant] Loading all strategies.");
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
            this.tradeManager = new IBGWTradeManager(serviceProvider);
            pollingIntervalInMs = config.PollingIntervalInMs;
            logger.LogInfo("Successfully initialized Quant.");
        }

        public void Run()
        {
            logger.LogInfo($"[Quant::Run] Starting main loop with polling interval: {pollingIntervalInMs}");
            var ledger = serviceProvider.GetRequiredService<ILedger>();
            while (!shouldTerminate)
            {
                ProcessSignals(ledger);
                logger.LogInfo($"Sleeping for {pollingIntervalInMs}ms until next iteration");
                Thread.Sleep(pollingIntervalInMs);
            }
        }

        public void Terminate()
        {
            logger.LogInfo($"[Quant] Signal to terminate application received. Gracefully shutting down after completing current iteration.");
            shouldTerminate = true;
        }

        private void Inititalize()
        {
            var services = new ServiceCollection();
            serviceProvider = services
            .AddSingleton<IConfig, Config.Config>()
            .AddSingleton<ILogger, Logger>(provider =>
            {
                var logger = provider.GetRequiredService<IConfig>();
                return new Logger(config);
            })
            .AddSingleton<ILedger, Ledger.Ledger>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                var config = provider.GetRequiredService<IConfig>();
                return new Ledger.Ledger(config, logger);
            })
            .AddSingleton<IIBGWClient, IBGWClient>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWClient(logger);
            })
            .AddSingleton<AlpacaMarketsClient>(provider =>
            {
                var config = provider.GetRequiredService<IConfig>();
                return new AlpacaMarketsClient(config);
            })
            .AddSingleton<IHistoricalMarketDataProvider, StockIndicatorsHistoricalDataProvider>(provider =>
            {
                var alpacaClient = provider.GetRequiredService<AlpacaMarketsClient>();
                var logger = provider.GetRequiredService<ILogger>();
                return new StockIndicatorsHistoricalDataProvider(alpacaClient, logger);
            })
            .AddSingleton<IAccountDataProvider, IBGWAccountDataProvider>(provider =>
            {
                var client = provider.GetRequiredService<IBGWClient>();
                var config = provider.GetRequiredService<IConfig>();
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWAccountDataProvider(client, config, logger);
            })
            .AddSingleton<IMarketDataProvider, IBGWMarketDataProvider>(provider =>
            {
                var client = provider.GetRequiredService<IBGWClient>();
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWMarketDataProvider(client, logger);
            })
            .BuildServiceProvider();
        }

        private async void ProcessSignals(ILedger ledger)
        {
            logger.LogInfo("[Quant::ProcessSignals] Processing Signals ...");

            var openPositions = ledger.GetOpenPositions();

            var retiredTickers = openPositions.Where(openTicker => !config.TickerStrategyMap.Keys.Contains(openTicker.Symbol));

            if (retiredTickers.Any())
            {
                logger.LogInfo($"Found {retiredTickers.Count()} symbols with open positions but no strategy defined. Loading the original strategy from positions in SellOnly mode");
                foreach (var ticker in retiredTickers)
                {
                    this.strategyProcessor.LoadStrategyFromContent(ticker.Symbol, ticker.StrategyDefinition);
                    this.strategyProcessor.SetStrategyStateForSymbol(ticker.Symbol, StrategyState.SellOnly);
                }
            }

            var accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
            var filteredPositions = new List<Position>();
            foreach (var position in openPositions)
            {
                if (await marketDataProvider.IsWithinTradingHours(position.Symbol, DateTime.UtcNow))
                {
                    logger.LogInfo($"[Quant::ProcessSignals] Not processing exit signals for symbol: {position.Symbol} since it is outside its trading hours");
                    filteredPositions.Add(position);
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
                        "Strategy",
                        "StrategyDefinition"
                    });
                var strategy = this.strategyProcessor.GetStrategy(position.Symbol);
                if (strategy.State != StrategyState.Halted)
                {
                    var marketData = await marketDataProvider.GetMarketDataAsync(position.Symbol);
                    var histData = historicalMarketDataProvider.GetHistoricalDataAsync(position.Symbol).Result;
                    ProcessExitSignal(position, marketData, histData);
                }
                else
                {
                    logger.LogInfo($"[Quant::ProcessSignals] Strategy {strategy.Name} for symbol {position.Symbol} is halted. Not processing any exit signals");
                }
            }

            if (this.riskManager.shouldHaltNewTrades(accountData))
            {
                logger.LogInfo($"[Quant::ProcesSignals] OPENING NEW POSITIONS HALTED! PLEASE CHECK PORTFOLIO STATE.");
                return;
            }

            List<Position> positionsOpened = new List<Position>();
            foreach (var symbol in config.TickerStrategyMap.Keys)
            {
                bool isWithinTradingHours = await marketDataProvider.IsWithinTradingHours(symbol, DateTime.UtcNow);
                if (!isWithinTradingHours)
                {
                    logger.LogInfo($"[Quant::ProcessSignals] Not processing entry signals for symbol: {symbol} since it is outside its trading hours.");
                    continue;
                }
                var strategy = this.strategyProcessor.GetStrategy(symbol);

                if (strategy.State == StrategyState.Active)
                {
                    var marketData = await marketDataProvider.GetMarketDataAsync(symbol);
                    var histData = historicalMarketDataProvider.GetHistoricalDataAsync(symbol).Result;
                    ProcessEntrySignal(symbol, positionsOpened, marketData, histData);
                }
                else
                {
                    logger.LogInfo($"[Quant::ProcessSignals] Strategy {strategy.Name} for symbol {symbol} is halted. Not processing any entry signals");
                }
            }
        }

        private async void ProcessExitSignal(Position position, MarketData marketData, HistoricalMarketData histData)
        {
            logger.LogInfo($"[Quant::ProcessExitSignal] Processing exit signals for {position.Symbol}");

            // Update current price according to latest market data for position
            position.CurrentPrice = marketData.LatestPrice;
            if (strategyProcessor.ShouldClose(marketData, accountData, histData, position))
            {
                logger.LogInfo($"[Quant::ProcessExitSignal] Exit conditions met for position: {position}");

                // For now, we don't engage the risk manager to close positions
                // We just sell off the entire position
                // TODO: Hard-coding in the order type as a market sell, need to support others
                var result = await tradeManager.ClosePositionAsync(position, OrderType.Market);
                // TODO: Handle unsuccessful transactions
                if (result.TransactionState == TransactionState.Completed)
                {
                    accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
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
                        "Strategy",
                        "StrategyDefinition"
                    });
                }
                else
                {
                    logger.LogInfo($"[Quant::ProcessExitSignal] Failed to close position: {position}");
                }
            }
            else
            {
                logger.LogInfo($"[Quant::ProcessExitSignal] Exit conditions weren't met for position: {position}");
            }

            logger.LogInfo($"[Quant::ProcessExitSignal] Successfully processed exit signals for position: {position}");
        }

        private async void ProcessEntrySignal(string symbol, IList<Position> positionsOpened, MarketData marketData, HistoricalMarketData histData)
        {
            logger.LogInfo($"[Quant::ProcessEntrySignal] Processing Entry Signals for {symbol}");
            if (strategyProcessor.ShouldOpen(marketData, accountData, histData, symbol))
            {
                var position = PrepareOpenPosition(symbol, marketData);
                if (riskManager.ComputePositionSize(marketData, histData, accountData, position))
                {
                    // TODO: Current only using market buys. Need to support other types like limit buys
                    var result = await tradeManager.OpenPositionAsync(position, OrderType.Market);
                    if (result.TransactionState == TransactionState.Completed)
                    {
                        accountData = await accountDataProvider.GetAccountDataAsync(config.AccountId);
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
                                "Strategy",
                                "StrategyDefinition"
                            });
                        positionsOpened.Add(position);
                        logger.LogInfo($"[Quant::ProcessEntrySignal] Successfully opened position: {position}");
                    }
                    else
                    {
                        logger.LogError($"[Quant::ProcessEntrySignal] Failed to open position:\n{position}");
                    }
                }
                else
                {
                    logger.LogInfo($"[Quant::ProcessEntrySignal] Appropriate resources not available to open position:\n {position}");
                }
            }
            else
            {
                logger.LogInfo($"[Quant::ProcessEntrySignal] Entry conditions weren't met for symbol {symbol}");
            }

            logger.LogInfo($"[Quant::ProcessEntrySignal] Successfully processed entry signals for symbol: {symbol}");
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
    }
}