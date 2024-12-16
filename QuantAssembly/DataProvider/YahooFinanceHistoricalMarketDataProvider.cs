using QuantAssembly.Impl.YahooFinance;
using QuantAssembly.Logging;
using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public class YahooHistoricalMarketDataProvider : IHistoricalMarketDataProvider
    {
        private readonly YahooFinanceAPIClient yahooClient;
        private readonly ILogger logger;

        public YahooHistoricalMarketDataProvider(YahooFinanceAPIClient yahooClient, ILogger logger)
        {
            this.yahooClient = yahooClient;
            this.logger = logger;
        }

        public HistoricalMarketData GetHistoricalData(string ticker, DateTime startDate, DateTime endDate)
        {
            try
            {
                logger.LogInfo($"Fetching historical data for {ticker} from {startDate} to {endDate}");
                var historicalDataTask = yahooClient.GetHistoricalDataAsync(ticker, startDate, endDate);
                historicalDataTask.Wait();
                var historicalData = historicalDataTask.Result;
                logger.LogInfo($"Successfully retrieved historical data for {ticker}");
                return historicalData;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error fetching historical data for {ticker}: {ex.Message}");
                logger.LogError(ex);
                throw;
            }
        }
    }
}