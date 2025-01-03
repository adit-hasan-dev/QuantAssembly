using IBApi;
using QuantAssembly.Logging;

namespace QuantAssembly.Impl.IBGW
{
    internal class ContractDetailsEventHandler : BaseEventHandler<ContractDetails>
    {
        public string symbolId { get; set; }
        private ILogger logger;
        private int requestId;

        public ContractDetailsEventHandler(
            string symbol,
            int requestId,
            TaskCompletionSource<ContractDetails> tcs,
            EWrapperImpl wrapper,
            EClientSocket eClientSocket,
            ILogger logger)
        {
            symbolId = symbol;
            this.taskCompletionSource = tcs;
            this.clientSocket = eClientSocket;
            this.eWrapper = wrapper;
            this.logger = logger;
            this.requestId = requestId;
        }

        public void ContractDetailsReceivedHandler(int reqId, ContractDetails contractDetails)
        {
            // The IBGWClient only supports getting the contract details for one ticker at a time
            // So we don't wait for contractDetailsEnd notification, we exit as soon as we have 
            // the contract we want
            if (contractDetails.Contract.Symbol.Equals(symbolId, StringComparison.OrdinalIgnoreCase))
            {
                taskCompletionSource.SetResult(contractDetails);
                Detach();
            }
        }

        public override void ErrorReceivedHandler(int id, int errorCode, string errorMsg, string advancedOrderRejectJson)
        {
            if (id == requestId)
            {
                var errorMessage = $"Id: {id} errorCode: {errorCode}. {errorMsg}. advancedOrderRejectJson: {advancedOrderRejectJson}";
                logger.LogError($"[IBGWClient::ContractDetails::ErrorReceivedHandler] {errorMessage}");
                
                taskCompletionSource.SetException(new Exception(errorMessage));
                Detach();
            }
        }

        protected override void Detach()
        {
            eWrapper.ContractDetailsReceived -= ContractDetailsReceivedHandler;
            eWrapper.ErrorReceived -= ErrorReceivedHandler;
        }
    }
}