using IBApi;
using QuantAssembly.Common.Logging;
using QuantAssembly.Core.Models;

namespace QuantAssembly.Impl.IBGW
{
    internal class AccountDataEventHandler : BaseEventHandler<AccountData>
    {
        private int requestId;
        public AccountData accountData { get; set; }
        private ILogger logger;

        public AccountDataEventHandler(
            int requestId,
            AccountData accountData,
            TaskCompletionSource<AccountData> taskCompletionSource,
            EWrapperImpl wrapper,
            EClientSocket eClientSocket,
            ILogger logger)
        {
            this.accountData = accountData;
            this.taskCompletionSource = taskCompletionSource;
            this.clientSocket = eClientSocket;
            this.eWrapper = wrapper;
            this.requestId = requestId;
            this.logger = logger;
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
                clientSocket.cancelAccountSummary(requestId); // Cancel the account summary request
                Detach();
            }
        }
        public override void ErrorReceivedHandler(int id, int errorCode, string errorMsg, string advancedOrderRejectJson)
        {
            if (id != requestId)
            {
                return;
            }

            var errorMessage = $"Id: {id} errorCode: {errorCode}. {errorMsg}. advancedOrderRejectJson: {advancedOrderRejectJson}";
            logger.LogError($"[IBGWClient::AccountData::ErrorReceivedHandler] {errorMessage}");
            
            taskCompletionSource.SetException(new Exception(errorMessage));
            clientSocket.cancelAccountSummary(requestId);
            Detach();
        }

        protected override void Detach()
        {
            eWrapper.AccountSummaryReceived -= AccountSummaryReceivedHandler;
            eWrapper.ErrorReceived -= ErrorReceivedHandler;
        }
    }
}