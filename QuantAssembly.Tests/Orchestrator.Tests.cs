using Newtonsoft.Json;
using QuantAssembly.Logging;
using QuantAssembly.Models;
using QuantAssembly.Orchestratration;

namespace QuantAssembly.Tests
{
    [TestClass]
    public class OrchestratorTests
    {
        private Orchestrator orchestrator;
        private ILogger logger;

        [TestInitialize]
        public void Setup()
        {
            logger = new Logger(new Config.Config());
            orchestrator = new Orchestrator(logger);
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

            orchestrator.LoadStrategy(stockTicker, filePath);

            MarketData marketData = new MarketData
            {
                LatestPrice = 120,
                MACD = 1.5,
                Signal = 2.0,
                RSI = 60,
                BidPrice = 95,
                AskPrice = 155
            };

            var accountData = new AccountData
            {
                AccountID = "ACC123456",
                TotalPortfolioValue = 250000.00,
                Liquidity = 50000.00,
                Equity = 200000.00,
            };

            // Act
            bool result = orchestrator.ShouldOpen(marketData, accountData, stockTicker);

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

            orchestrator.LoadStrategy(stockTicker, filePath);

            MarketData marketData = new MarketData
            {
                LatestPrice = 80,
                MACD = 1.5,
                Signal = 2.0,
                RSI = 75,
                BidPrice = 85,
                AskPrice = 155
            };

            var accountData = new AccountData
            {
                AccountID = "ACC123456",
                TotalPortfolioValue = 250000.00,
                Liquidity = 50000.00,
                Equity = 200000.00,
            };

            // Act
            bool result = orchestrator.ShouldClose(marketData, accountData, stockTicker);

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
            string filePath = "AAPL_strategy.json";
            File.WriteAllText(filePath, strategyJson);

            // Act
            orchestrator.LoadStrategy(stockTicker, filePath);

            // Assert: Expects an InvalidOperationException to be thrown
        }
    }
}
