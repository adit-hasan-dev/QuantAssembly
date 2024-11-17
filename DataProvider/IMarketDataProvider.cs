using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public interface IMarketDataProvider
    {
        double GetLatestPrice(string ticker);
        double GetRSI(string ticker);
        (double MACD, double Signal) GetMACD(string ticker);
        MarketData GetMarketData(string ticker);
        void SubscribeMarketData(string ticker);
    }
}
