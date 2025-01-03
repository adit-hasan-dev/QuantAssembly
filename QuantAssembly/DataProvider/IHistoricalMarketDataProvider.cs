using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public interface IHistoricalMarketDataProvider
    {
        Task<HistoricalMarketData> GetHistoricalDataAsync(string ticker);
    }
}