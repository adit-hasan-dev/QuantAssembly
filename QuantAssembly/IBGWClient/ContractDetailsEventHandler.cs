using IBApi;

namespace QuantAssembly.Impl.IBGW
{
    internal class ContractDetailsEventHandler : BaseEventHandler<ContractDetails>
    {
        public string symbolId { get; set; }

        public ContractDetailsEventHandler(
            string symbol,
            TaskCompletionSource<ContractDetails> tcs,
            EWrapperImpl wrapper,
            EClientSocket eClientSocket)
        {
            symbolId = symbol;
            this.taskCompletionSource = tcs;
            this.clientSocket = eClientSocket;
            this.eWrapper = wrapper;
        }

        public void ContractDetailsReceivedHandler(int reqId, ContractDetails contractDetails)
        {
            // The IBGWClient only supports getting the contract details for one ticker at a time
            // So we don't wait for contractDetailsEnd notification, we exit as soon as we have 
            // the contract we want
            if (contractDetails.Contract.Symbol.Equals(symbolId, StringComparison.OrdinalIgnoreCase))
            {
                taskCompletionSource.SetResult(contractDetails);
                eWrapper.ContractDetailsReceived -= ContractDetailsReceivedHandler;
            }
        }
    }
}