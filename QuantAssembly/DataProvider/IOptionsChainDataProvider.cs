using QuantAssembly.Common.Models;

namespace QuantAssembly.DataProvider
{
    // TODO: Move to QuantAssembly.Common
    public interface IOptionsChainDataProvider
    {
        Task<List<OptionsContractData>> GetOptionsChainDataAsync(
            string symbol,
            double minimumExpirationTimeInDays);
    }
}