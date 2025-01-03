using IBApi;
using QuantAssembly.Models;

namespace QuantAssembly.Impl.IBGW
{
    internal class AccountDataEventHandler : BaseEventHandler<AccountData>
    {
        public int requestId { get; set; }
        public AccountData accountData { get; set; }

        public AccountDataEventHandler(
            AccountData accountData,
            TaskCompletionSource<AccountData> taskCompletionSource,
            EWrapperImpl wrapper,
            EClientSocket eClientSocket)
        {
            this.accountData = accountData;
            this.taskCompletionSource = taskCompletionSource;
            this.clientSocket = eClientSocket;
            this.eWrapper = wrapper;
        }

        public void AccountSummaryReceivedHandler(
            string reqId,
            string account,
            string tag,
            string value,
            string currency)
        {
            if (!string.Equals(account, accountData.AccountID, StringComparison.OrdinalIgnoreCase))
                return;

            switch (tag)
            {
                case "NetLiquidation":
                    accountData.TotalPortfolioValue = double.Parse(value);
                    break;
                case "TotalCashValue":
                    accountData.Liquidity = double.Parse(value);
                    break;
                case "GrossPositionValue":
                    accountData.Equity = double.Parse(value);
                    break;
            }

            if (accountData.TotalPortfolioValue != -10 && accountData.Liquidity != -10 && accountData.Equity != -10)
            {
                taskCompletionSource.SetResult(accountData);
                eWrapper.AccountSummaryReceived -= AccountSummaryReceivedHandler;
                clientSocket.cancelAccountSummary(1); // Cancel the account summary request
            }
        }

    }
}