namespace QuantAssembly.Models
{
    public class MarketData
    {
        public string Symbol { get; set;}
        public double LatestPrice { get; set;}
        public double AskPrice { get; set;}
        public double BidPrice { get; set;}
        public double RSI { get; set;}
        public double MACD { get; set;}
        public double Signal { get; set;}
        public double SMA { get; set;}
    }

    public class StockData : MarketData
    {

    }
}