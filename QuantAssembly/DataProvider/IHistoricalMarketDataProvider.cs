using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public interface IHistoricalMarketDataProvider
    {
        HistoricalMarketData GetHistoricalData(string ticker, DateTime startDate, DateTime endDate);
    }
}