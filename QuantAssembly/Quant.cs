using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.DataProvider;
using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using Newtonsoft.Json;
using QuantAssembly.Common.Ledger;
using QuantAssembly.Core.Strategy;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core;
using QuantAssembly.Core.RiskManagement;
using QuantAssembly.Core.Models;

namespace QuantAssembly
{
    public class Quant : TradingEngine
    {
        private bool shouldTerminate;
        private int pollingIntervalInMs;
        private IMarketDataProvider marketDataProvider;

        public Quant()
        {
            logger.LogInfo("Initializing Quant...");

            this.marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();

            pollingIntervalInMs = config.PollingIntervalInMs;
            logger.LogInfo("Successfully initialized Quant.");
        }

        public override async Task Run()
        {
            var pipeline = new PipelineBuilder<QuantContext>(this.serviceProvider, this.config)
                .AddStep<InitStep>()
                .AddStep<GenerateExitSignalsStep>()
                .AddStep<GenerateEntrySignalsStep>()
                .AddStep<ClosePositionsStep>()
                .AddStep<RiskManagementStep>()
                .AddStep<OpenPositionsStep>()
                .Build();

            var strategyProcessor = serviceProvider.GetRequiredService<IStrategyProcessor>();
            InitializeStrategies(strategyProcessor, ledger);
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

        protected override void InitializeDependencies(ServiceCollection services)
        {
            services
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
            });
        }

        private void InitializeStrategies(IStrategyProcessor strategyProcessor, ILedger ledger)
        {
            logger.LogInfo($"[{nameof(Quant)}] Loading all strategies.");
            strategyProcessor = new StrategyProcessor(logger);
            foreach (var (ticker, strategyPath) in config.TickerStrategyMap)
            {
                try
                {
                    strategyProcessor.LoadStrategyFromFile(ticker, strategyPath);
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
                    strategyProcessor.LoadStrategyFromContent(ticker.Symbol, ticker.StrategyDefinition);
                    strategyProcessor.SetStrategyStateForSymbol(ticker.Symbol, StrategyState.SellOnly);
                }
            }
            logger.LogInfo($"[{nameof(Quant)}] Successfully loaded {strategyProcessor.GetLoadedInstruments().Count} strategies");
        }
    }
}