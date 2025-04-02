using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.BackTesting.DataProvider;
using QuantAssembly.BackTesting.TradeManager;
using QuantAssembly.BackTesting.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.DataProvider;
using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Common;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.TradeManager;
using QuantAssembly.Common.Ledger;
using QuantAssembly.Core;

namespace QuantAssembly.BackTesting
{
    public class BacktestConfig
    {
        public double InitialPortfolioValue { get; set; }

    }
    public class BackTestEngine : TradingEngine<Config>
    {
        private ServiceProvider serviceProvider;
        private TimePeriod timePeriod;
        private StepSize stepSize;
        private Config config;
        private ILogger logger;

        private BacktestConfig backtestConfig;

        public override async Task Run()
        {
            var pipeline = new PipelineBuilder<BacktestContext>(this.serviceProvider, this.config)
                .AddStep<InitStep>()
                .AddStep<GenerateExitSignalsStep>()
                .AddStep<GenerateEntrySignalsStep>()
                .AddStep<ClosePositionsStep>()
                .AddStep<RiskManagementStep>()
                .AddStep<OpenPositionsStep>()
                .AddStep<UpdateSummaryStep>()
                .Build();
            
            logger.LogInfo($"[BackTestEngine::Run] Starting main loop with polling interval: {this.stepSize} and time period: {this.timePeriod}");
            var timeMachine = serviceProvider.GetRequiredService<TimeMachine>();
            // TODO: init summary
            while (timeMachine.GetCurrentTime() < timeMachine.endTime)
            {
                try
                {
                    await pipeline.Execute();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }

            logger.LogInfo($"[BackTestEngine::Run] Completed backtesting period");
            logger.LogInfo(pipeline.GetContext().backtestSummary.ToString());
        }

        protected override void InitializeDependencies(ServiceCollection services)
        {
            this.logger.LogInfo($"[{nameof(BackTestEngine)}] Initializing dependencies");
            services
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
            .AddSingleton<TimeMachine>(provider =>
            {
                var startTime = DateTime.UtcNow.Subtract(Common.Utility.GetTimeSpanFromTimePeriod(timePeriod)).Subtract(TimeSpan.FromMinutes(16));
                return new TimeMachine(this.timePeriod, this.stepSize, startTime);
            })
            .AddSingleton<ITimeProvider>(provider =>
            {
                var timeMachine = provider.GetRequiredService<TimeMachine>();
                return new SimulatedTimeProvider(timeMachine);
            })
            .AddSingleton<BacktestMarketDataProvider>(provider =>
            {
                var timeMachine = provider.GetRequiredService<TimeMachine>();
                var alpacaClient = provider.GetRequiredService<AlpacaMarketsClient>();
                var timeProvider = provider.GetRequiredService<ITimeProvider>();

                return new BacktestMarketDataProvider(timeMachine, timeProvider, alpacaClient, logger, config.CacheFolderPath, config.TickerStrategyMap.Keys.ToList());
            })
            .AddSingleton<IIndicatorDataProvider, BacktestMarketDataProvider>(provider =>
            {
                return provider.GetRequiredService<BacktestMarketDataProvider>();
            })
            .AddSingleton<IMarketDataProvider, BacktestMarketDataProvider>(provider =>
            {
                return provider.GetRequiredService<BacktestMarketDataProvider>();
            })
            .AddSingleton<IAccountDataProvider, BacktestAccountDataProvider>(provider =>
            {
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
            });
            this.logger.LogInfo($"[{nameof(BackTestEngine)}] Successfully initialized dependencies");
        }
    }
}