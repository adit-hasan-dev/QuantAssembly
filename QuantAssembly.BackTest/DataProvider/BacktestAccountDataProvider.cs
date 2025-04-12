using QuantAssembly.Core.DataProvider;
using QuantAssembly.Core.Models;
using QuantAssembly.Models;

namespace QuantAssembly.BackTesting.DataProvider
{
    public class BacktestAccountDataProvider : IAccountDataProvider
    {
        private AccountData accountData;

        public BacktestAccountDataProvider(string accountId, double initialTotalPortfolioValue)
        {
            accountData = new AccountData
            {
                AccountID = accountId,
                TotalPortfolioValue = initialTotalPortfolioValue,
                Liquidity = initialTotalPortfolioValue,
                Equity = 0
            };
        }

        public Task<AccountData> GetAccountDataAsync(string accountId)
        {
            if (accountData.AccountID == accountId)
            {
                return Task.FromResult(accountData);
            }
            else
            {
                throw new ArgumentException("Account ID not found.");
            }
        }

        public void SetAccountData(AccountData newAccountData)
        {
            if (newAccountData != null)
            {
                accountData = newAccountData;
            }
        }
    }

}