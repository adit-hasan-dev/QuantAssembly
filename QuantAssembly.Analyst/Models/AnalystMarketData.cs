namespace QuantAssembly.Analyst.Models
{
    public class AnalystMarketData
    {
        public string Symbol { get; set; }
        public double LatestPrice { get; set; }
        public double AskPrice { get; set; }
        public double BidPrice { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Volume { get; set; }
        public double Vwap { get; set; }
        public double TradeCount { get; set; }
    }
}