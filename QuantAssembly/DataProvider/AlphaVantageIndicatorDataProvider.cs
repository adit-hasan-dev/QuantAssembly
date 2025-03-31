using QuantAssembly.Impl.AlphaVantage;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Core.DataProvider;

namespace QuantAssembly.DataProvider
{
    public class AlphaVantageIndicatorDataProvider : IIndicatorDataProvider
    {
        private readonly AlphaVantageClient alphaVantageClient;
        private readonly ILogger logger;

        public AlphaVantageIndicatorDataProvider(AlphaVantageClient alphaVantageClient, ILogger logger)
        {
            this.alphaVantageClient = alphaVantageClient;
            this.logger = logger;
        }

        public async Task<IndicatorData> GetIndicatorDataAsync(string symbol)
        {
            try
            {
                logger.LogInfo($"Fetching indicator market data for {symbol}.");

                var rsi = await alphaVantageClient.GetRSIAsync(symbol);
                var sma50 = await alphaVantageClient.GetSMAAsync(symbol, 50);
                var sma200 = await alphaVantageClient.GetSMAAsync(symbol, 200);
                var ema50 = await alphaVantageClient.GetEMAAsync(symbol, 50);
                var (macd, signal) = await alphaVantageClient.GetMACDAsync(symbol);
                var (upperBand, lowerBand) = await alphaVantageClient.GetBollingerBandsAsync(symbol);
                var atr = await alphaVantageClient.GetATRAsync(symbol);
                var (historicalHigh, historicalLow) = await alphaVantageClient.GetHistoricalHighLowAsync(symbol);

                var IndicatorData = new IndicatorData
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

                logger.LogInfo($"Successfully fetched and populated indicator market data for {symbol}");
                return IndicatorData;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error fetching indicator market data for {symbol}: {ex.Message}");
                logger.LogError(ex);
                throw;
            }
        }
    }

}

