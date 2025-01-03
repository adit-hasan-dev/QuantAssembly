using Newtonsoft.Json;
using QuantAssembly.Logging;
using QuantAssembly.Models;
using QuantAssembly.Strategy;

namespace QuantAssembly.Tests
{
    [TestClass]
    public class StrategyProcessorTests
    {
        private StrategyProcessor strategyProcessor;
        private ILogger logger;

        [TestInitialize]
        public void Setup()
        {
            var config = new Config.Config();
            logger = new Logger(config);
            strategyProcessor = new StrategyProcessor(logger);
        }

        [TestMethod]
        public void Test_ShouldOpen_WithValidConditions()
        {
            // Arrange
            string strategyJson = @"
            {
                ""EntryConditions"": {
                    ""LogicalOperator"": ""AND"",
                    ""Conditions"": [
                        {
                            ""Property"": ""LatestPrice"",
                            ""Operator"": ""GreaterThan"",
                            ""Value"": 100
                        },
                        {
                            ""Operator"": ""LessThan"",
                            ""LeftHandOperand"": ""MACD"",
                            ""RightHandOperand"": ""SignalLine""
                        }
                    ]
                },
                ""ExitConditions"": {
                    ""LogicalOperator"": ""OR"",
                    ""Conditions"": [
                        {
                            ""Property"": ""RSI"",
                            ""Operator"": ""GreaterThan"",
                            ""Value"": 70
                        }
                    ]
                },
                ""StopLossConditions"": {
                    ""LogicalOperator"": ""AND"",
                    ""Conditions"": [
                        {
                            ""Property"": ""BidPrice"",
                            ""Operator"": ""LessThan"",
                            ""Value"": 90
                        }
                    ]
                },
                ""TakeProfitConditions"": {
                    ""LogicalOperator"": ""OR"",
                    ""Conditions"": [
                        {
                            ""Property"": ""AskPrice"",
                            ""Operator"": ""GreaterThan"",
                            ""Value"": 150
                        }
                    ]
                }
            }";
            string stockTicker = "AAPL";
            string filePath = "AAPL_strategy.json";
            File.WriteAllText(filePath, strategyJson);

            strategyProcessor.LoadStrategyFromContent(stockTicker, strategyJson);

            MarketData marketData = new MarketData
            {
                LatestPrice = 120,
                BidPrice = 95,
                AskPrice = 155
            };

            HistoricalMarketData histData = new HistoricalMarketData
            {
                MACD = 1.5,
                Signal = 2.0,
                RSI = 60,
            };

            var accountData = new AccountData
            {
                AccountID = "ACC123456",
                TotalPortfolioValue = 250000.00,
                Liquidity = 50000.00,
                Equity = 200000.00,
            };

            // Act
            bool result = strategyProcessor.ShouldOpen(marketData, accountData, histData, stockTicker);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Test_ShouldClose_WithValidConditions()
        {
            // Arrange
            string strategyJson = @"
    {
        ""EntryConditions"": {
            ""LogicalOperator"": ""AND"",
            ""Conditions"": [
                {
                    ""Property"": ""LatestPrice"",
                    ""Operator"": ""GreaterThan"",
                    ""Value"": 100
                },
                {
                    ""Operator"": ""LessThan"",
                    ""LeftHandOperand"": ""MACD"",
                    ""RightHandOperand"": ""SignalLine""
                }
            ]
        },
        ""ExitConditions"": {
            ""LogicalOperator"": ""OR"",
            ""Conditions"": [
                {
                    ""Property"": ""RSI"",
                    ""Operator"": ""GreaterThan"",
                    ""Value"": 70
                },
                {
                    ""Property"": ""LossPercentage"",
                    ""Operator"": ""GreaterThan"",
                    ""Value"": 5
                },
                {
                    ""Property"": ""ProfitPercentage"",
                    ""Operator"": ""GreaterThan"",
                    ""Value"": 10
                }
            ]
        },
        ""StopLossConditions"": {
            ""LogicalOperator"": ""AND"",
            ""Conditions"": [
                {
                    ""Property"": ""BidPrice"",
                    ""Operator"": ""LessThan"",
                    ""Value"": 90
                }
            ]
        },
        ""TakeProfitConditions"": {
            ""LogicalOperator"": ""OR"",
            ""Conditions"": [
                {
                    ""Property"": ""AskPrice"",
                    ""Operator"": ""GreaterThan"",
                    ""Value"": 150
                }
            ]
        }
    }";
            string stockTicker = "AAPL";

            strategyProcessor.LoadStrategyFromContent(stockTicker, strategyJson);

            // Set valid open and current prices to calculate profit/loss
            Position position = new Position
            {
                PositionGuid = Guid.NewGuid(),
                Symbol = stockTicker,
                OpenPrice = 100, // Assume the open price is 100
                CurrentPrice = 110, // Assume the current price is 110 to calculate profit
                Quantity = 10
            };

            MarketData marketData = new MarketData
            {
                LatestPrice = 80,
                BidPrice = 85,
                AskPrice = 155
            };

            HistoricalMarketData histData = new HistoricalMarketData
            {
                MACD = 1.5,
                Signal = 2.0,
                RSI = 75,
            };

            var accountData = new AccountData
            {
                AccountID = "ACC123456",
                TotalPortfolioValue = 250000.00,
                Liquidity = 50000.00,
                Equity = 200000.00,
            };

            // Act
            bool result = strategyProcessor.ShouldClose(marketData, accountData, histData, position);

            // Assert
            Assert.IsTrue(result);
        }


        [TestMethod]
        [ExpectedException(typeof(JsonSerializationException))]
        public void Test_LoadStrategy_InvalidStrategy_ThrowsException()
        {
            // Arrange
            string strategyJson = @"
            {
                ""EntryConditions"": {
                    ""LogicalOperator"": ""AND"",
                    ""Conditions"": [
                        {
                            ""Property"": ""InvalidProperty"",
                            ""Operator"": ""GreaterThan"",
                            ""Value"": 100
                        }
                    ]
                },
                ""ExitConditions"": {
                    ""LogicalOperator"": ""OR"",
                    ""Conditions"": []
                },
                ""StopLossConditions"": {
                    ""LogicalOperator"": ""AND"",
                    ""Conditions"": []
                },
                ""TakeProfitConditions"": {
                    ""LogicalOperator"": ""OR"",
                    ""Conditions"": []
                }
            }";
            string stockTicker = "AAPL";

            // Act
            strategyProcessor.LoadStrategyFromContent(stockTicker, strategyJson);

            // Assert: Expects an InvalidOperationException to be thrown
        }
    }
}
