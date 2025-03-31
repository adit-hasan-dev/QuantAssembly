
using QuantAssembly.Common.Models;

namespace QuantAssembly.Core.DataProvider
{
    // TODO: Move to QuantAssembly.Common
    public interface IIndicatorDataProvider
    {
        Task<IndicatorData> GetIndicatorDataAsync(string ticker);
    }
}