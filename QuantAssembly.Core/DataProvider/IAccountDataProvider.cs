using QuantAssembly.Models;

namespace QuantAssembly.Core.DataProvider
{
    public interface IAccountDataProvider
    {
        Task<AccountData> GetAccountDataAsync(string accountId);
    }
}
