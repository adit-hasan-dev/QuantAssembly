using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.DataProvider;
using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Common.Logging;
using QuantAssembly.Strategy;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.RiskManagement;
using QuantAssembly.Models;
using Newtonsoft.Json;
using QuantAssembly.Common.Ledger;
using QuantAssembly.Core.Strategy;
using QuantAssembly.Core.DataProvider;

namespace QuantAssembly
{
    public class Quant
    {
        private Config config;
        private ILogger logger;
        private bool shouldTerminate;
        private int pollingIntervalInMs;
        private ServiceProvider serviceProvider;
        private IMarketDataProvider marketDataProvider;
        private ILedger ledger;

        public Quant()
        {
            this.config = ConfigurationLoader.LoadConfiguration<Models.Config>();
            InitializeDependencies();
            logger = serviceProvider.GetRequiredService<ILogger>();
            ledger = serviceProvider.GetRequiredService<ILedger>();

            logger.LogInfo("Initializing Quant...");

            this.marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();

            pollingIntervalInMs = config.PollingIntervalInMs;
            logger.LogInfo("Successfully initialized Quant.");
        }

        public async Task Run()
        {
            var pipeline = new PipelineBuilder<QuantContext>(this.serviceProvider, this.config)
                .AddStep<InitStep>()
                .AddStep<GenerateExitSignalsStep>()
                .AddStep<GenerateEntrySignalsStep>()
                .AddStep<ClosePositionsStep>()
                .AddStep<RiskManagementStep>()
                .AddStep<OpenPositionsStep>()
                .Build();

            InitializeStrategies(pipeline.GetContext(), ledger);
            logger.LogInfo($"[ReQuant::Run] Starting main loop with polling interval: {pollingIntervalInMs}");

            while (!shouldTerminate)
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
            logger.LogInfo($"[Quant] Signal to terminate application received. Gracefully shutting down after completing current iteration.");
            shouldTerminate = true;
        }

        private void InitializeDependencies()
        {
            var services = new ServiceCollection();
            serviceProvider = services
            .AddSingleton<ILogger, Logger>(provider =>
            {
                return new Logger(this.config, isDevEnv: false);
            })
            .AddSingleton<ILedger, Ledger.Ledger>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                return new Ledger.Ledger(this.config.LedgerFilePath, logger);
            })
            .AddSingleton<IIBGWClient, IBGWClient>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWClient(logger);
            })
            .AddSingleton<AlpacaMarketsClient>(provider =>
            {
                if (config.CustomProperties.TryGetValue(nameof(AlpacaMarketsClientConfig), out var alpacaConfigJson))
                {
                    var alpacaConfig = JsonConvert.DeserializeObject<AlpacaMarketsClientConfig>(alpacaConfigJson.ToString());
                    return new AlpacaMarketsClient(alpacaConfig);
                }
                else
                {
                    throw new Exception("AlpacaMarketsClientConfig not found in config");
                }
            })
            .AddSingleton<IIndicatorDataProvider, StockIndicatorsDataProvider>(provider =>
            {
                var alpacaClient = provider.GetRequiredService<AlpacaMarketsClient>();
                var logger = provider.GetRequiredService<ILogger>();
                return new StockIndicatorsDataProvider(alpacaClient, logger);
            })
            .AddSingleton<IAccountDataProvider, IBGWAccountDataProvider>(provider =>
            {
                var client = provider.GetRequiredService<IIBGWClient>();
                var logger = provider.GetRequiredService<ILogger>();
                return new IBGWAccountDataProvider(client, logger);
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
                if (this.config.CustomProperties.TryGetValue(nameof(PercentageAccountValueRiskManagerConfig), out var riskManagerConfigJson))
                {
                    var percentageRiskManagerConfig = JsonConvert.DeserializeObject<PercentageAccountValueRiskManagerConfig>(riskManagerConfigJson.ToString());
                    return new PercentageAccountValueRiskManager(provider, this.config.RiskManagement, percentageRiskManagerConfig);
                }
                else
                {
                    throw new Exception("PercentageAccountValueRiskManagerConfig not found in config");
                }
            })
            .BuildServiceProvider();
        }

        private void InitializeStrategies(QuantContext context, ILedger ledger)
        {
            logger.LogInfo($"[{nameof(Quant)}] Loading all strategies.");
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