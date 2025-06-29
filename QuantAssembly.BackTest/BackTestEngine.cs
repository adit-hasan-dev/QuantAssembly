using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.BackTesting.DataProvider;
using QuantAssembly.BackTesting.TradeManager;
using QuantAssembly.BackTesting.Models;
using QuantAssembly.DataProvider;
using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Common;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.TradeManager;
using QuantAssembly.Common.Ledger;
using QuantAssembly.Core;
using QuantAssembly.Core.Strategy;
using QuantAssembly.Common.Models;
using QuantAssembly.Core.RiskManagement;
using QuantAssembly.Core.Pipeline;

namespace QuantAssembly.BackTesting
{
    public class BacktestConfig
    {
        public double InitialPortfolioValue { get; init; }
        public TimePeriod timePeriod { get; init; } = TimePeriod.OneYear;
        public StepSize stepSize { get; init; } = StepSize.OneHour;
    }

    public class BackTestEngine : TradingEngine
    {
        private const TimePeriod timePeriod = TimePeriod.OneYear;
        private const StepSize stepSize = StepSize.ThirtyMinutes;

        private BacktestConfig backtestConfig;

        public override async Task Run()
        {
            var pipeline = new PipelineBuilder<BacktestContext>(this.serviceProvider, this.config)
                .AddStep<InitStep<BacktestContext>>()
                .AddStep<GenerateExitSignalsStep<BacktestContext>>()
                .AddStep<GenerateEntrySignalsStep<BacktestContext>>()
                .AddStep<ClosePositionsStep<BacktestContext>>()
                .AddStep<RiskManagementStep<BacktestContext>>()
                .AddStep<OpenPositionsStep<BacktestContext>>()
                .Build();

            var timeMachine = serviceProvider.GetRequiredService<TimeMachine>();
            var marketDataProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
            var strategyProcessor = serviceProvider.GetRequiredService<IStrategyProcessor>();
            this.InitializeStrategies(strategyProcessor, marketDataProvider);

            logger.LogInfo($"[BackTestEngine::Run] Starting main loop with polling interval: {BackTestEngine.stepSize} and time period: {BackTestEngine.timePeriod}");
            while (timeMachine.GetCurrentTime() < timeMachine.endTime)
            {
                try
                {
                    logger.LogInfo($"[BackTestEngine::Run] Current time: {timeMachine.GetCurrentTime()}");
                    await pipeline.Execute();
                    timeMachine.StepForward();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }

            logger.LogInfo($"[BackTestEngine::Run] Completed backtesting period");
            logger.LogInfo(GenerateBacktestSummary().ToString());
        }

        protected override void InitializeDependencies(ServiceCollection services)
        {
            this.logger.LogInfo($"[{nameof(BackTestEngine)}] Initializing dependencies");
            if (config.CustomProperties.TryGetValue(nameof(BacktestConfig), out var backTestConfigJson))
            {
                this.backtestConfig = JsonConvert.DeserializeObject<BacktestConfig>(backTestConfigJson.ToString()) ?? new BacktestConfig();
            }
            else
            {
                throw new InvalidOperationException($"Configuration for {nameof(BacktestConfig)} not found.");
            }

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
                return new TimeMachine(BackTestEngine.timePeriod, BackTestEngine.stepSize, startTime);
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
                return new BacktestAccountDataProvider(config.AccountId, this.backtestConfig.InitialPortfolioValue);
            })
            .AddSingleton<ITradeManager, BacktestTradeManager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                var ledger = provider.GetRequiredService<ILedger>();
                var accountDataProvider = provider.GetRequiredService<IAccountDataProvider>() as BacktestAccountDataProvider;
                var timeMachine = provider.GetRequiredService<TimeMachine>();
                return new BacktestTradeManager(ledger, logger, accountDataProvider, timeMachine, config.AccountId);
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
            .AddSingleton<IStrategyProcessor, StrategyProcessor>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                return new StrategyProcessor(logger);
            });
            this.logger.LogInfo($"[{nameof(BackTestEngine)}] Successfully initialized dependencies");
        }

        private void InitializeStrategies(IStrategyProcessor strategyProcessor, IMarketDataProvider marketDataProvider)
        {
            logger.LogInfo($"[{nameof(BackTestEngine)}] Loading all strategies.");
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

            // We don't expect there to be any retired tickers when backtesting
            marketDataProvider.FlushMarketDataCache();
            logger.LogInfo($"[{nameof(BackTestEngine)}] Successfully loaded {strategyProcessor.GetLoadedInstruments().Count} strategies");
        }

        private BackTestSummary GenerateBacktestSummary()
        {
            this.logger.LogInfo($"[{nameof(BackTestEngine)}] Generating backtest summary");

            var ledger = this.serviceProvider.GetRequiredService<ILedger>();
            var openPositions = ledger.GetOpenPositions();
            var closedPositions = ledger.GetClosedPositions();

            var finalPortfolioValue = closedPositions.Sum(p => p.ProfitOrLoss) +
                                      openPositions.Sum(p => (p.CurrentPrice - p.OpenPrice) * p.Quantity) +
                                      this.backtestConfig.InitialPortfolioValue;

            var allPositions = openPositions.Concat(closedPositions);
            var strategyReports = allPositions
                .GroupBy(p => p.StrategyName)
                .Select(group =>
                {
                    var strategyName = group.Key;
                    var positions = group.ToList();
                    var closedPositionsForStrategy = positions.Where(p => p.State == PositionState.Closed).ToList();

                    var takeProfitPositions = closedPositionsForStrategy
                        .Where(p => p.CloseReason == ClosePositionReason.TakeProfitLevelHit)
                        .ToList();
                    var stopLossPositions = closedPositionsForStrategy
                        .Where(p => p.CloseReason == ClosePositionReason.StopLossLevelHit)
                        .ToList();

                    var profits = closedPositionsForStrategy.Select(p => p.ProfitOrLoss).ToList();
                    var cumulativeReturns = profits.Aggregate(new List<double> { 0.0 }, (acc, x) =>
                    {
                        acc.Add(acc.Last() + x);
                        return acc;
                    });

                    // Use the CalculateStandardDeviation method from Utils.cs
                    var standardDeviation = profits.Any() ? Common.Utility.CalculateStandardDeviation(profits) : 0;

                    return new BacktestStrategyReport
                    {
                        StrategyName = strategyName,
                        StopLossConditionsHit = stopLossPositions.Count,
                        ExitConditionsHit = closedPositionsForStrategy.Count(p => p.CloseReason == ClosePositionReason.ExitConditionHit),
                        EntryConditionsHit = positions.Count,
                        TakeProfitConditionsHit = takeProfitPositions.Count,
                        PositionsStillOpen = positions.Count(p => p.State == PositionState.Open),
                        AverageProfitOnTakeProfit = takeProfitPositions.Any() ? takeProfitPositions.Average(p => p.ProfitOrLoss) : 0,
                        AverageLossOnStopLoss = stopLossPositions.Any() ? stopLossPositions.Average(p => p.ProfitOrLoss) : 0,
                        AverageHoldingPeriodInDays = closedPositionsForStrategy.Any()
                            ? closedPositionsForStrategy.Average(p => (p.CloseTime - p.OpenTime).TotalDays)
                            : 0,
                        WinRate = closedPositionsForStrategy.Any()
                            ? (double)closedPositionsForStrategy.Count(p => p.ProfitOrLoss > 0) / closedPositionsForStrategy.Count
                            : 0,
                        LossRate = closedPositionsForStrategy.Any()
                            ? (double)closedPositionsForStrategy.Count(p => p.ProfitOrLoss < 0) / closedPositionsForStrategy.Count
                            : 0,
                        MaxDrawdown = cumulativeReturns.Any()
                            ? cumulativeReturns.Zip(cumulativeReturns.Skip(1), (prev, curr) => curr - prev).Min()
                            : 0,
                        SharpeRatio = standardDeviation > 0
                            ? profits.Average() / standardDeviation
                            : 0,
                        ProfitFactor = profits.Any(p => p > 0) && profits.Any(p => p < 0)
                            ? profits.Where(p => p > 0).Sum() / Math.Abs(profits.Where(p => p < 0).Sum())
                            : 0,
                        AverageReturnPerTrade = profits.Any() ? profits.Average() : 0,
                        LargestSingleTradeProfit = profits.Any() ? profits.Max() : 0,
                        LargestSingleTradeLoss = profits.Any() ? profits.Min() : 0
                    };
                })
                .ToList();

            return new BackTestSummary
            {
                InitialPortfolioValue = this.backtestConfig.InitialPortfolioValue,
                FinalPortfolioValue = finalPortfolioValue,
                backtestStrategyReports = strategyReports
            };
        }
    }
}