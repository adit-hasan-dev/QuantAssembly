using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Ledger;
using QuantAssembly.Logging;
using QuantAssembly.Models;
using QuantAssembly.Models.Constants;
using QuantAssembly.TradeManager;

namespace QuantAssembly.Tests
{
    [TestClass]
    public class IBGWTradeManagerTests
    {
        private IBGWTradeManager? tradeManager;
        private MockIBGWClient? mockClient;
        private ILogger? logger;
        private ILedger? ledger;
        private ServiceProvider? serviceProvider;

        [TestInitialize]
        public void Setup()
        {
            var config = new Config.Config();
            if (File.Exists(config.LedgerFilePath))
            {
                File.WriteAllText(config.LedgerFilePath, string.Empty);
            }
            else
            {
                File.Create(config.LedgerFilePath).Dispose();
            }

            logger = new Logger(config, isDevEnv: true);
            ledger = new Ledger.Ledger(config, logger);
            mockClient = new MockIBGWClient();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IIBGWClient>(mockClient);
            serviceCollection.AddSingleton<ILogger>(logger);
            serviceCollection.AddSingleton<ILedger>(ledger);
            serviceProvider = serviceCollection.BuildServiceProvider();
            tradeManager = new IBGWTradeManager(serviceProvider);
        }

        [TestMethod]
        public async Task Test_OpenAndClosePositionAsync()
        {
            // Open Position
            var position = new Position
            {
                Symbol = "AAPL",
                InstrumentType = InstrumentType.Stock,
                Quantity = 10,
                Currency = "USD"
            };

            // Check initial state
            Assert.AreEqual(0, ledger?.GetOpenPositions().Count);
            Assert.AreEqual(0, ledger?.GetClosedPositions().Count);

            var openResult = await tradeManager?.OpenPositionAsync(position, OrderType.Market);

            // Validate position was opened and added to ledger
            Assert.AreEqual(TransactionState.Completed, openResult.TransactionState);
            Assert.AreEqual(100, position.OpenPrice);  // Expected simulated price
            Assert.AreEqual(PositionState.Open, position.State);
            Assert.AreEqual(1, ledger?.GetOpenPositions().Count);
            Assert.AreEqual(0, ledger?.GetClosedPositions().Count);

            // Close Position
            var closeResult = await tradeManager.ClosePositionAsync(position, OrderType.Market);

            // Validate position was closed and removed from ledger
            Assert.AreEqual(TransactionState.Completed, closeResult.TransactionState);
            Assert.AreEqual(100, position.ClosePrice);  // Expected simulated price
            Assert.AreEqual(PositionState.Closed, position.State);
            Assert.AreEqual(DateTime.Now.Date, position.CloseTime.Date);
            Assert.AreEqual(0, ledger?.GetOpenPositions().Count);
            Assert.AreEqual(1, ledger?.GetClosedPositions().Count);
        }
    }

}