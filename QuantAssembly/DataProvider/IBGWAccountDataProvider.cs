using QuantAssembly.Config;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Logging;
using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public class IBGWAccountDataProvider : IAccountDataProvider
    {
        private readonly IBGWClient iBGWClient;
        private readonly ManualResetEvent accountSummaryEvent = new ManualResetEvent(false);
        private AccountData accountData;

        private ILogger logger;

        public IBGWAccountDataProvider(IBGWClient client, IConfig config ,ILogger logger)
        {
            this.logger = logger;
            this.iBGWClient = client;
            this.iBGWClient.AccountSummaryReceived += OnAccountSummaryReceived;
            this.accountData = new AccountData { AccountID = config.AccountId };
        }

        public AccountData GetAccountData()
        {
            logger.LogInfo("Getting Account Data Events");
            accountSummaryEvent.Reset();
            iBGWClient.RequestAccountSummary();
            accountSummaryEvent.WaitOne(); // Wait for the account summary to be received

            return accountData;
        }

        private void OnAccountSummaryReceived(string reqId, string account, string tag, string value, string currency)
        {
            if (!string.Equals(account, this.accountData.AccountID, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            switch (tag)
            {
                case "NetLiquidation":
                    logger.LogDebug($"Updating total portfolio value: {value} {currency}");
                    accountData.TotalPortfolioValue = double.Parse(value);
                    break;
                case "TotalCashValue":
                    logger.LogDebug($"Updating total cash value: {value} {currency}");
                    accountData.Liquidity = double.Parse(value);
                    break;
                case "GrossPositionValue":
                    logger.LogDebug($"Updating total equity value: {value} {currency}");
                    accountData.Equity = double.Parse(value);
                    break;
            }

            // Assuming all required tags are received in one callback, otherwise implement appropriate logic
            if (accountData.Equity != 0 && accountData.TotalPortfolioValue != 0 && accountData.Equity != 0)
            {
                accountSummaryEvent.Set();
            }
        }
    }
}