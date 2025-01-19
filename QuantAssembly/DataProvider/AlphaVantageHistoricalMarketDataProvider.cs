using System;
using System.Threading.Tasks;
using QuantAssembly.Impl.AlphaVantage;
using QuantAssembly.Common.Logging;
using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public class AlphaVantageHistoricalMarketDataProvider : IHistoricalMarketDataProvider
    {
        private readonly AlphaVantageClient alphaVantageClient;
        private readonly ILogger logger;

        public AlphaVantageHistoricalMarketDataProvider(AlphaVantageClient alphaVantageClient, ILogger logger)
        {
            this.alphaVantageClient = alphaVantageClient;
            this.logger = logger;
        }

        public async Task<HistoricalMarketData> GetHistoricalDataAsync(string symbol)
        {
            try
            {
                logger.LogInfo($"Fetching historical market data for {symbol}.");

                var rsi = await alphaVantageClient.GetRSIAsync(symbol);
                var sma50 = await alphaVantageClient.GetSMAAsync(symbol, 50);
                var sma200 = await alphaVantageClient.GetSMAAsync(symbol, 200);
                var ema50 = await alphaVantageClient.GetEMAAsync(symbol, 50);
                var (macd, signal) = await alphaVantageClient.GetMACDAsync(symbol);
                var (upperBand, lowerBand) = await alphaVantageClient.GetBollingerBandsAsync(symbol);
                var atr = await alphaVantageClient.GetATRAsync(symbol);
                var (historicalHigh, historicalLow) = await alphaVantageClient.GetHistoricalHighLowAsync(symbol);

                var historicalMarketData = new HistoricalMarketData
                {
                    Symbol = symbol,
                    RSI = rsi,
                    SMA_50 = sma50,
                    SMA_200 = sma200,
                    EMA_50 = ema50,
                    MACD = macd,
                    Signal = signal,
                    Upper_Band = upperBand,
                    Lower_Band = lowerBand,
                    ATR = atr,
                    HistoricalHigh = historicalHigh,
                    HistoricalLow = historicalLow
                };

                logger.LogInfo($"Successfully fetched and populated historical market data for {symbol}");
                return historicalMarketData;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error fetching historical market data for {symbol}: {ex.Message}");
                logger.LogError(ex);
                throw;
            }
        }
    }

}

