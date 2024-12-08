using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Config;
using QuantAssembly.DataProvider;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Ledger;
using QuantAssembly.Logging;
using QuantAssembly.Orchestratration;

namespace QuantAssembly
{
    public class Quant
    {
        private IConfig config;
        private ILogger logger;

        private ServiceProvider serviceProvider;

        private Orchestrator orchestrator;

        private int pollingIntervalInMs;

        public Quant()
        {
            Inititalize();
            logger = serviceProvider.GetRequiredService<ILogger>();
            config = serviceProvider.GetRequiredService<IConfig>();
            logger.LogInfo("Initializing Quant...");

            // Load all strategies
            logger.LogInfo($"[Quant] Loading all strategies.");
            this.orchestrator = new Orchestrator(logger);
            foreach (var (ticker, strategyPath) in config.TickerStrategyMap)
            {
                this.orchestrator.LoadStrategy(ticker, strategyPath);
            }
            pollingIntervalInMs = config.PollingIntervalInMs;
            logger.LogInfo("Successfully initialized Quant.");
        }

        public void Run()
        {
            using (var client = new IBGWClient(logger))
            {
                logger.LogInfo("[Quant] Setting up main loop");

                logger.LogInfo($"Subscribing to market data for all tickers...");
                var marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
                foreach (string ticker in config.TickerStrategyMap.Keys)
                {
                    marketDataProvider.SubscribeMarketData(ticker);
                }
                logger.LogInfo("[Quant] Successfully set up main loop");

                logger.LogInfo($"Starting main loop with polling interval: {pollingIntervalInMs}");
                bool shouldStop = false;
                while (!shouldStop)
                {
                    ProcessExitSignals();
                    ProcessEntrySignals();
                    logger.LogInfo($"Sleeping for {pollingIntervalInMs}ms until next iteration");
                    Thread.Sleep(pollingIntervalInMs);
                }
            }
        }

        private void Inititalize()
        {
            var services = new ServiceCollection();
            serviceProvider = services
            .AddSingleton<IConfig, Config.Config>()
            .AddSingleton<ILogger, Logger>(provider => {
                var logger = provider.GetRequiredService<IConfig>();
                return new Logger(config);
            })
            .AddSingleton<ILedger, Ledger.Ledger>(provider => {
                var logger = provider.GetRequiredService<ILogger>();
                var config = provider.GetRequiredService<IConfig>();
                return new Ledger.Ledger(config, logger);
            })
            .AddSingleton<IBGWClient>(provider => {
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWClient(logger);
            })
            .AddSingleton<IAccountDataProvider, IBGWAccountDataProvider>(provider => {
                var client = provider.GetRequiredService<IBGWClient>();
                var config = provider.GetRequiredService<IConfig>();
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWAccountDataProvider(client, config, logger);
            })
            .AddSingleton<IMarketDataProvider, IBGWMarketDataProvider>(provider => {
                var client = provider.GetRequiredService<IBGWClient>();
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWMarketDataProvider(client, logger);
            })
            .BuildServiceProvider();
        }

        private void ProcessExitSignals()
        {
            logger.LogInfo("Processing Exit Signals ...");
            logger.LogInfo("Successfully processed Exit Signals");
        }

        private void ProcessEntrySignals()
        {
            logger.LogInfo("Processing Entry Signals ...");
            logger.LogInfo("Successfully processed Entry Signals");
        }
    }
}