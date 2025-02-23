using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.DataProvider;
using QuantAssembly.Impl.AlpacaMarkets;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Ledger;
using QuantAssembly.Common.Logging;
using QuantAssembly.Strategy;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.RiskManagement;

namespace QuantAssembly
{
    public class ReQuant
    {
        private IConfig config;
        private ILogger logger;
        private bool shouldTerminate;
        private int pollingIntervalInMs;
        private ServiceProvider serviceProvider;
        private IMarketDataProvider marketDataProvider;
        private ILedger ledger;
        
        public ReQuant()
        {
            InitializeDependencies();
            logger = serviceProvider.GetRequiredService<ILogger>();
            config = serviceProvider.GetRequiredService<IConfig>();
            ledger = serviceProvider.GetRequiredService<ILedger>();

            logger.LogInfo("Initializing Quant...");

            this.marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
            
            pollingIntervalInMs = config.PollingIntervalInMs;
            logger.LogInfo("Successfully initialized Quant.");
        }

        public async Task Run()
        {
            var pipeline = new PipelineBuilder<QuantContext>(this.serviceProvider)
                .AddStep<InitStep>()
                .AddStep<GenerateExitSignalsStep>()
                .AddStep<GenerateEntrySignalsStep>()
                .AddStep<ClosePositionsStep>()
                .AddStep<RiskManagementStep>()
                .AddStep<OpenPositionsStep>()
                .Build();

            InitializeStrategies(pipeline.GetContext(), ledger);
            logger.LogInfo($"[ReQuant::Run] Starting main loop with polling interval: {pollingIntervalInMs}");

            while(!shouldTerminate)
            {
                try
                {
                    await pipeline.Execute();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }

                logger.LogInfo($"Sleeping for {pollingIntervalInMs}ms until next iteration");
                await Task.Delay(pollingIntervalInMs);
            }
        }

        public void Terminate()
        {
            logger.LogInfo($"[ReQuant] Signal to terminate application received. Gracefully shutting down after completing current iteration.");
            shouldTerminate = true;
        }

        private void InitializeDependencies()
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
                var client = provider.GetRequiredService<IIBGWClient>();
                var config = provider.GetRequiredService<IConfig>();
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWAccountDataProvider(client, config, logger);
            })
            .AddSingleton<IMarketDataProvider, IBGWMarketDataProvider>(provider =>
            {
                var client = provider.GetRequiredService<IIBGWClient>();
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWMarketDataProvider(client, logger);
            })
            .AddSingleton<IRiskManager, PercentageAccountValueRiskManager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                return new PercentageAccountValueRiskManager(provider);
            })
            .BuildServiceProvider();
        }

        private void InitializeStrategies(QuantContext context, ILedger ledger)
        {
            context.strategyProcessor = new StrategyProcessor(logger);
            foreach (var (ticker, strategyPath) in config.TickerStrategyMap)
            {
                try
                {
                    context.strategyProcessor.LoadStrategyFromFile(ticker, strategyPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }

            var openPositions = ledger.GetOpenPositions();
            this.marketDataProvider.FlushMarketDataCache();

            var retiredTickers = openPositions.Where(openTicker => !config.TickerStrategyMap.Keys.Contains(openTicker.Symbol));

            if (retiredTickers.Any())
            {
                logger.LogInfo($"Found {retiredTickers.Count()} symbols with open positions but no strategy defined. Loading the original strategy from positions in SellOnly mode");
                foreach (var ticker in retiredTickers)
                {
                    context.strategyProcessor.LoadStrategyFromContent(ticker.Symbol, ticker.StrategyDefinition);
                    context.strategyProcessor.SetStrategyStateForSymbol(ticker.Symbol, StrategyState.SellOnly);
                }
            }
        }
    }
}