namespace QuantAssembly.Models
{
    public class MarketData
    {
        public string Symbol { get; set; }
        public double LatestPrice { get; set; }
        public double AskPrice { get; set; }
        public double BidPrice { get; set; }
    }

    /// <summary>
    /// Represents historical market data for a financial instrument.
    /// This data is calculated over a period of time and includes various indicators.
    /// 
    /// Time Interval: Daily
    /// Resolution: 14, 50, 200 days (based on specific indicators)
    /// 
    /// Note:
    /// - The latest values for indicators are retrieved from appropriate data sources.
    /// - HistoricalHigh and HistoricalLow are calculated using price data for a specified period.
    /// </summary>
    public class IndicatorData
    {
        /// <summary>
        /// Gets or sets the symbol of the financial instrument.
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Gets or sets the Relative Strength Index (RSI) calculated over a 14-day period.
        /// </summary>
        public double RSI { get; set; }

        /// <summary>
        /// Gets or sets the Simple Moving Average (SMA) calculated over a 50-day period.
        /// </summary>
        public double SMA_50 { get; set; }

        /// <summary>
        /// Gets or sets the Simple Moving Average (SMA) calculated over a 200-day period.
        /// </summary>
        public double SMA_200 { get; set; }

        /// <summary>
        /// Gets or sets the Exponential Moving Average (EMA) calculated over a 50-day period.
        /// </summary>
        public double EMA_50 { get; set; }

        /// <summary>
        /// Gets or sets the Moving Average Convergence Divergence (MACD).
        /// </summary>
        public double MACD { get; set; }

        /// <summary>
        /// Gets or sets the MACD signal line.
        /// </summary>
        public double Signal { get; set; }

        /// <summary>
        /// Gets or sets the upper Bollinger Band calculated over a 20-day period.
        /// </summary>
        public double Upper_Band { get; set; }

        /// <summary>
        /// Gets or sets the lower Bollinger Band calculated over a 20-day period.
        /// </summary>
        public double Lower_Band { get; set; }

        /// <summary>
        /// Gets or sets the Average True Range (ATR) calculated over a 14-day period.
        /// </summary>
        public double ATR { get; set; }

        /// <summary>
        /// Gets or sets the highest historical price calculated from a specified period.
        /// </summary>
        public double HistoricalHigh { get; set; }

        /// <summary>
        /// Gets or sets the lowest historical price calculated from a specified period.
        /// </summary>
        public double HistoricalLow { get; set; }
    }
}