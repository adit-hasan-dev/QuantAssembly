using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using QuantAssembly.Common.Config;
using QuantAssembly.RiskManagement;
using QuantAssembly.Common.Logging;
using QuantAssembly.Models;
using QuantAssembly.Models.Constants;

namespace TradingApp.Tests
{
    public class TestConfig : IConfig
    {
        public string AccountId => "DU9275937";
        public bool EnableDebugLog => true;
        public string LogFilePath => "app.log";
        public double GlobalStopLoss => 5000;
        public double MaxDrawDownPercentage => 0.4;
        public Dictionary<string, string> TickerStrategyMap => new Dictionary<string, string>
        {
            {"SPY", "strategies/SPY_strategy.json"},
            {"AAPL", "strategies/AAPL_strategy.json"},
            {"GOOGL", "strategies/GOOGL_strategy.json"}
        };
        public string LedgerFilePath => "ledger.json";
        public string CacheFolderPath => "cachefoler";
        public int PollingIntervalInMs => 1000;
        public string APIKey => "<key>";
        public RiskManagementConfig RiskManagement => new RiskManagementConfig
        {
            GlobalStopLoss = this.GlobalStopLoss,
            MaxDrawDownPercentage = this.MaxDrawDownPercentage
        };
        public Dictionary<string, JObject> CustomProperties => new Dictionary<string, JObject>
        {
            { "PercentageAccountValueRiskManagerConfig", JObject.FromObject(new PercentageAccountValueRiskManagerConfig { MaxTradeRiskPercentage = 0.02 }) }
        };
    }

    public class TestLogger : ILogger
    {
        public void LogInfo(string message) => System.Diagnostics.Debug.WriteLine(message);
        public void LogError(string message) => System.Diagnostics.Debug.WriteLine(message);
        public void LogDebug(string message) => System.Diagnostics.Debug.WriteLine(message);
        public void LogError(Exception exception) => System.Diagnostics.Debug.WriteLine(exception.Message);
        public void LogWarn(string message) => System.Diagnostics.Debug.WriteLine(message);
    }

    [TestClass]
    public class PercentageAccountValueRiskManagerTests
    {
        private ServiceProvider serviceProvider;

        [TestInitialize]
        public void Setup()
        {
            // Setup ServiceProvider with TestConfig and TestLogger
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfig, TestConfig>();
            serviceCollection.AddSingleton<ILogger, TestLogger>();
            serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [TestMethod]
        public void ComputePositionSize_ShouldReturnTrue_WhenPositionSizeIsCalculated()
        {
            // Arrange
            var riskManager = new PercentageAccountValueRiskManager(serviceProvider);
            var marketData = new MarketData
            {
                Symbol = "AAPL",
                LatestPrice = 100
            };
            var accountData = new AccountData
            {
                TotalPortfolioValue = 10000,
                Liquidity = 10000,
                Equity = 0
            };
            Position position = new Position
            {
                PositionGuid = Guid.NewGuid(),
                Symbol = "AAPL",
                InstrumentType = InstrumentType.Stock,
                State = PositionState.Open,
                StrategyName = "StrategyA",
                StrategyDefinition = "StrategyDefinition"
            };

            // Act
            var result = riskManager.ComputePositionSize(marketData, null, accountData, position);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("AAPL", position.Symbol);
            Assert.AreEqual(2, position.Quantity); // Based on MaxTradeRiskPercentage
        }

        [TestMethod]
        public void ComputePositionSize_ShouldReturnFalse_WhenNotEnoughCapital()
        {
            // Arrange
            var riskManager = new PercentageAccountValueRiskManager(serviceProvider);
            var marketData = new MarketData { Symbol = "AAPL", LatestPrice = 10000 };
            var accountData = new AccountData { TotalPortfolioValue = 10000 };
            Position position = new Position();

            // Act
            var result = riskManager.ComputePositionSize(marketData, null, accountData, position);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldHaltNewTrades_ShouldReturnTrue_WhenGlobalStopLossIsReached()
        {
            // Arrange
            var riskManager = new PercentageAccountValueRiskManager(serviceProvider);
            var accountData = new AccountData { TotalPortfolioValue = 4000 };

            // Act
            var result = riskManager.ShouldHaltNewTrades(accountData);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ShouldHaltNewTrades_ShouldReturnFalse_WhenGlobalStopLossIsNotReached()
        {
            // Arrange
            var riskManager = new PercentageAccountValueRiskManager(serviceProvider);
            var accountData = new AccountData { TotalPortfolioValue = 10000 };

            // Act
            var result = riskManager.ShouldHaltNewTrades(accountData);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
