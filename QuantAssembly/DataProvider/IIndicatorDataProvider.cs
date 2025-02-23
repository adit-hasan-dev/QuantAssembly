using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public interface IIndicatorDataProvider
    {
        Task<IndicatorData> GetIndicatorDataAsync(string ticker);
    }
}