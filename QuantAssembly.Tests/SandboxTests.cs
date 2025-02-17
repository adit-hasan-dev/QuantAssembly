using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantAssembly.DataProvider;
using QuantAssembly.Impl.AlpacaMarkets;
using QuantAssembly.Impl.AlphaVantage;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Config;

namespace QuantAssembly.Tests
{
    [TestClass]
    [Ignore]
    public class SandboxTests
    {
        [TestMethod]
        public async Task Test_AlphaVantageAPIClient()
        {
            var config = new Config();
            var logger = new Logger(config, isDevEnv: true);
            var apiKey = "key"; //config.APIKey;
            var alphaVantageClient = new AlphaVantageClient(logger, apiKey);
            var ticker = "AAPL";

            try
            {
                // Fetch historical market data
                var rsi = await alphaVantageClient.GetRSIAsync(ticker);
                var sma50 = await alphaVantageClient.GetSMAAsync(ticker, 50);
                var sma200 = await alphaVantageClient.GetSMAAsync(ticker, 200);
                var ema50 = await alphaVantageClient.GetEMAAsync(ticker, 50);
                var shortTermEMA = await alphaVantageClient.GetEMAAsync(ticker, 12);
                var longTermEMA = await alphaVantageClient.GetEMAAsync(ticker, 26);
                var (upperBand, lowerBand) = await alphaVantageClient.GetBollingerBandsAsync(ticker);
                var atr = await alphaVantageClient.GetATRAsync(ticker);
                var (historicalHigh, historicalLow) = await alphaVantageClient.GetHistoricalHighLowAsync(ticker);
                var (macd, signal) = await alphaVantageClient.GetMACDAsync(ticker);

                // Compute MACD and Signal Line
                double macdLine = shortTermEMA - longTermEMA;

                // Validate the data
                Assert.IsNotNull(rsi);
                Assert.IsNotNull(sma50);
                Assert.IsNotNull(sma200);
                Assert.IsNotNull(ema50);
                Assert.IsNotNull(shortTermEMA);
                Assert.IsNotNull(longTermEMA);
                Assert.IsNotNull(macdLine);
                Assert.IsNotNull(upperBand);
                Assert.IsNotNull(lowerBand);
                Assert.IsNotNull(atr);
                Assert.IsNotNull(historicalHigh);
                Assert.IsNotNull(historicalLow);
                Assert.IsNotNull(macd);
                Assert.IsNotNull(signal);

                // Log the data
                logger.LogInfo($"Ticker: {ticker}");
                logger.LogInfo($"RSI: {rsi}");
                logger.LogInfo($"SMA_50: {sma50}");
                logger.LogInfo($"SMA_200: {sma200}");
                logger.LogInfo($"EMA_50: {ema50}");
                logger.LogInfo($"Short-Term EMA (12): {shortTermEMA}");
                logger.LogInfo($"Long-Term EMA (26): {longTermEMA}");
                logger.LogInfo($"MACD: {macdLine}");
                logger.LogInfo($"Signal: {signal}");
                logger.LogInfo($"Upper_Band: {upperBand}");
                logger.LogInfo($"Lower_Band: {lowerBand}");
                logger.LogInfo($"ATR: {atr}");
                logger.LogInfo($"Historical High: {historicalHigh}");
                logger.LogInfo($"Historical Low: {historicalLow}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"An error occurred: {ex.Message}, {ex.StackTrace}");
            }
        }

        [TestMethod]
        public async Task Test_AlpacaMarketsClient()
        {
            var config = new Config();
            var logger = new Logger(config, isDevEnv: true);
            var alpacaMarketsClient = new AlpacaMarketsClient(config);
            var histDataProvider = new StockIndicatorsHistoricalDataProvider(alpacaMarketsClient, logger);
            var ticker = "AAPL";

            try
            {
                var indicators = await histDataProvider.GetHistoricalDataAsync(ticker);

                // Compute MACD and Signal Line

                // Validate the data
                Assert.IsNotNull(indicators.RSI);
                Assert.IsNotNull(indicators.SMA_50);
                Assert.IsNotNull(indicators.SMA_200);
                Assert.IsNotNull(indicators.EMA_50);
                Assert.IsNotNull(indicators.MACD);
                Assert.IsNotNull(indicators.Signal);
                Assert.IsNotNull(indicators.Upper_Band);
                Assert.IsNotNull(indicators.Lower_Band);
                Assert.IsNotNull(indicators.ATR);
                Assert.IsNotNull(indicators.HistoricalHigh);
                Assert.IsNotNull(indicators.HistoricalLow);

                // Log the data
                logger.LogInfo($"Ticker: {ticker}");
                logger.LogInfo($"RSI: {indicators.RSI}");
                logger.LogInfo($"SMA_50: {indicators.SMA_50}");
                logger.LogInfo($"SMA_200: {indicators.SMA_200}");
                logger.LogInfo($"EMA_50: {indicators.EMA_50}");
                logger.LogInfo($"MACD: {indicators.MACD}");
                logger.LogInfo($"Signal: {indicators.Signal}");
                logger.LogInfo($"Upper_Band: {indicators.Upper_Band}");
                logger.LogInfo($"Lower_Band: {indicators.Lower_Band}");
                logger.LogInfo($"ATR: {indicators.ATR}");
                logger.LogInfo($"Historical High: {indicators.HistoricalHigh}");
                logger.LogInfo($"Historical Low: {indicators.HistoricalLow}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"An error occurred: {ex.Message}, {ex.StackTrace}");
            }
        }

        [TestMethod]
        public async Task Test_IBGWClient()
        {
            var config = new Config();
            var logger = new Logger(config, isDevEnv: true);

            var client = new IBGWClient(logger);

            var marketData = await client.RequestMarketDataAsync("AAPL");
            var accountData = await client.RequestAccountSummaryAsync(config.AccountId);

            var marketDataMSFT = await client.RequestMarketDataAsync("MSFT");
            Assert.IsNotNull(marketData);
            Assert.IsNotNull(accountData);
            Assert.IsNotNull(marketDataMSFT);
        }

        [TestMethod]
        public async Task Test_IBGWClient_ContractDetails()
        {
            var config = new Config();
            var logger = new Logger(config, isDevEnv: true);

            var client = new IBGWClient(logger);
            var dataProvider = new IBGWMarketDataProvider(client, logger);
            var isWithinTradingHours = await dataProvider.IsWithinTradingHours("AAPL", DateTime.UtcNow);
            Assert.IsTrue(isWithinTradingHours);
        }
    }
}
