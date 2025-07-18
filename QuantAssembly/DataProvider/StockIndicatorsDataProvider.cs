using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Common.Logging;
using Skender.Stock.Indicators;
using Alpaca.Markets;
using QuantAssembly.Common.Models;
using QuantAssembly.Core.DataProvider;

namespace QuantAssembly.DataProvider
{
    public class StockIndicatorsDataProvider : IIndicatorDataProvider
    {
        private readonly AlpacaMarketsClient alpacaDataClient;
        private readonly ILogger logger;

        public StockIndicatorsDataProvider(AlpacaMarketsClient alpacaDataClient, ILogger logger)
        {
            this.alpacaDataClient = alpacaDataClient;
            this.logger = logger;
        }

        public async Task<IndicatorData> GetIndicatorDataAsync(string ticker)
        {
            var indicatorData = await alpacaDataClient.GetIndicatorDataAsync<IBar>(ticker);

            var quotes = indicatorData
                .Select(bar => new Quote
                {
                    Date = bar.TimeUtc,
                    Open = bar.Open,
                    High = bar.High,
                    Low = bar.Low,
                    Close = bar.Close,
                    Volume = bar.Volume
                })
            .OrderBy(x => x.Date); // optional

            var rsi = quotes.GetRsi(14).LastOrDefault()?.Rsi ?? 0;
            var sma50 = quotes.GetSma(50).LastOrDefault()?.Sma ?? 0;
            var sma200 = quotes.GetSma(200).LastOrDefault()?.Sma ?? 0;
            var ema50 = quotes.GetEma(50).LastOrDefault()?.Ema ?? 0;
            var macdResult = quotes.GetMacd(12, 26, 9).LastOrDefault();
            var macd = macdResult?.Macd ?? 0;
            var signal = macdResult?.Signal ?? 0;
            var bollingerBands = quotes.GetBollingerBands(20).LastOrDefault();
            var upperBand = bollingerBands?.UpperBand ?? 0;
            var lowerBand = bollingerBands?.LowerBand ?? 0;
            var atr = quotes.GetAtr(14).LastOrDefault()?.Atr ?? 0;
            var historicalHigh = quotes.Max(q => q.High);
            var historicalLow = quotes.Min(q => q.Low);

            return new IndicatorData
            {
                Symbol = ticker,
                RSI = rsi,
                SMA_50 = sma50,
                SMA_200 = sma200,
                EMA_50 = ema50,
                MACD = macd,
                Signal = signal,
                Upper_Band = upperBand,
                Lower_Band = lowerBand,
                ATR = atr,
                HistoricalHigh = (double)historicalHigh,
                HistoricalLow = (double)historicalLow
            };
        }
    }

}
