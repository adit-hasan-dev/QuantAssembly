using IBApi;
using QuantAssembly.DataProvider;
using QuantAssembly.Logging;

namespace QuantAssembly.Tests
{
    [TestClass]
    public class IBGWMarketDataProviderTests
    {
        private MockIBGWClient? mockClient;
        private IBGWMarketDataProvider? dataProvider;
        private ILogger? logger;


        [TestInitialize]
        public void Setup()
        {
            var config = new Config.Config();

            logger = new Logger(config, isDevEnv: true);
            mockClient = new MockIBGWClient();
            dataProvider = new IBGWMarketDataProvider(mockClient, logger);
        }

        [TestMethod]
        public async Task IsWithinTradingHours_ReturnsTrue_WithinTradingHours()
        {
            // Arrange
            var mockTradingHours = "20250102:0400-20250102:2000;20250103:0400-20250103:2000;20250104:CLOSED;20250105:CLOSED;20250106:0400-20250106:2000;20250107:0400-20250107:2000";
            var mockTimeZoneId = "US/Eastern";
            mockClient.contractDetails = new ContractDetails
            {
                TradingHours = mockTradingHours,
                TimeZoneId = mockTimeZoneId
            };

            // Set the current time to 10:00 AM EST
            var currentTime = new DateTime(2025, 1, 2, 15, 0, 0, DateTimeKind.Utc); // 10:00 AM EST in UTC

            // Act
            var isWithinTradingHours = await dataProvider?.IsWithinTradingHours("AAPL", currentTime);

            // Assert
            Assert.IsTrue(isWithinTradingHours);
        }

        [TestMethod]
        public async Task IsWithinTradingHours_ReturnsFalse_OutsideTradingHours()
        {
            // Arrange
            var mockTradingHours = "20250102:0400-20250102:2000;20250103:0400-20250103:2000;20250104:CLOSED;20250105:CLOSED;20250106:0400-20250106:2000;20250107:0400-20250107:2000";
            var mockTimeZoneId = "US/Eastern";
            mockClient.contractDetails = new ContractDetails
            {
                TradingHours = mockTradingHours,
                TimeZoneId = mockTimeZoneId
            };

            // Set the current time to 3:00 AM EST
            var currentTime = new DateTime(2025, 1, 2, 8, 0, 0, DateTimeKind.Utc); // 3:00 AM EST in UTC

            // Act
            var isWithinTradingHours = await dataProvider.IsWithinTradingHours("AAPL", currentTime);

            // Assert
            Assert.IsFalse(isWithinTradingHours);
        }

        [TestMethod]
        public async Task IsWithinTradingHours_ReturnsFalse_ClosedDay_PST()
        {
            // Arrange
            var mockTradingHours = "20250102:0400-20250102:2000;20250103:0400-20250103:2000;20250104:CLOSED;20250105:CLOSED;20250106:0400-20250106:2000;20250107:0400-20250107:2000";
            var mockTimeZoneId = "Pacific Standard Time";
            mockClient.contractDetails = new ContractDetails
            {
                TradingHours = mockTradingHours,
                TimeZoneId = mockTimeZoneId
            };

            // Set the current time to any time on a closed day (e.g., January 4th)
            var currentTime = new DateTime(2025, 1, 4, 12, 0, 0, DateTimeKind.Utc); // Noon UTC

            // Act
            var isWithinTradingHours = await dataProvider.IsWithinTradingHours("AAPL", currentTime);

            // Assert
            Assert.IsFalse(isWithinTradingHours);
        }


    }
}