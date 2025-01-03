using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public interface IAccountDataProvider
    {
        Task<AccountData> GetAccountDataAsync(string accountId);
    }
}
