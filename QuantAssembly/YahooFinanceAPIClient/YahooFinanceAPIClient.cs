using QuantAssembly.Logging;
using QuantAssembly.Models;

namespace QuantAssembly.Impl.YahooFinance
{
    public class YahooFinanceAPIClient
    {
        private readonly HttpClient httpClient;
        private readonly ILogger logger;

        public YahooFinanceAPIClient(ILogger logger)
        {
            httpClient = new HttpClient();
            this.logger = logger;
        }

        public async Task<HistoricalMarketData> GetHistoricalDataAsync(string ticker, DateTime startDate, DateTime endDate)
        {
            try
            {
                var startTimestamp = new DateTimeOffset(startDate).ToUnixTimeSeconds();
                var endTimestamp = new DateTimeOffset(endDate).ToUnixTimeSeconds();
                var url = $"https://query1.finance.yahoo.com/v7/finance/download/{ticker}?period1={startTimestamp}&period2={endTimestamp}&interval=1d&events=history";

                logger.LogInfo($"Requesting historical data for {ticker} from {startDate} to {endDate}");
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var csvData = await response.Content.ReadAsStringAsync();
                logger.LogInfo($"Received historical data for {ticker}");

                return ParseCsvData(ticker, csvData);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to retrieve historical data for {ticker}: {ex.Message}");
                logger.LogError(ex);
                throw;
            }
        }

        private HistoricalMarketData ParseCsvData(string ticker, string csvData)
        {
            try
            {
                var lines = csvData.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
                var historicalData = new HistoricalMarketData { Symbol = ticker };

                var closePrices = new List<double>();
                var highPrices = new List<double>();
                var lowPrices = new List<double>();

                foreach (var line in lines)
                {
                    var values = line.Split(',');

                    if (double.TryParse(values[4], out double closePrice))
                    {
                        closePrices.Add(closePrice);
                    }

                    if (double.TryParse(values[2], out double highPrice))
                    {
                        highPrices.Add(highPrice);
                    }

                    if (double.TryParse(values[3], out double lowPrice))
                    {
                        lowPrices.Add(lowPrice);
                    }
                }

                historicalData.HistoricalHigh = highPrices.Max();
                historicalData.HistoricalLow = lowPrices.Min();
                historicalData.SMA = CalculateSMA(closePrices, 14); // Example for 14-day SMA
                historicalData.RSI = CalculateRSI(closePrices, 14); // Example for 14-day RSI
                (historicalData.MACD, historicalData.Signal) = CalculateMACD(closePrices, 12, 26, 9); // Example MACD

                logger.LogInfo($"Parsed historical data for {ticker}");

                return historicalData;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to parse historical data for {ticker}: {ex.Message}");
                logger.LogError(ex);
                throw;
            }
        }

        private double CalculateSMA(List<double> prices, int period)
        {
            if (prices.Count < period) return double.NaN;
            return prices.Skip(prices.Count - period).Average();
        }

        private double CalculateRSI(List<double> prices, int period)
        {
            if (prices.Count < period) return double.NaN;

            double gain = 0, loss = 0;

            for (int i = prices.Count - period; i < prices.Count; i++)
            {
                double change = prices[i] - prices[i - 1];
                if (change > 0) gain += change;
                else loss -= change;
            }

            double averageGain = gain / period;
            double averageLoss = loss / period;

            double rs = averageGain / averageLoss;
            return 100 - (100 / (1 + rs));
        }

        private (double MACD, double Signal) CalculateMACD(List<double> prices, int shortPeriod, int longPeriod, int signalPeriod)
        {
            if (prices.Count < longPeriod + signalPeriod) return (double.NaN, double.NaN);

            var shortEMA = CalculateEMA(prices, shortPeriod);
            var longEMA = CalculateEMA(prices, longPeriod);

            var macdLine = shortEMA.Zip(longEMA, (shortE, longE) => shortE - longE).ToList();
            var signalLine = CalculateEMA(macdLine, signalPeriod);

            return (macdLine.Last(), signalLine.Last());
        }

        private List<double> CalculateEMA(List<double> prices, int period)
        {
            double multiplier = 2.0 / (period + 1);
            var ema = new List<double> { prices.Take(period).Average() };

            for (int i = period; i < prices.Count; i++)
            {
                double value = ((prices[i] - ema.Last()) * multiplier) + ema.Last();
                ema.Add(value);
            }

            return ema;
        }
    }
}