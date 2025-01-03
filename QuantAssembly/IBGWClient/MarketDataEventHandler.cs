using IBApi;
using QuantAssembly.Logging;
using QuantAssembly.Models;

namespace QuantAssembly.Impl.IBGW
{
    internal class MarketDataEventHandler : BaseEventHandler<MarketData>
    {
        public int requestId { get; set; }
        public MarketData marketData { get; set; }

        private ILogger logger;

        public MarketDataEventHandler(
            int requestId, 
            MarketData marketData,
            TaskCompletionSource<MarketData> taskCompletionSource,
            EWrapperImpl wrapper,
            EClientSocket eClientSocket,
            ILogger logger)
        {
            this.requestId = requestId;
            this.marketData = marketData;
            this.taskCompletionSource = taskCompletionSource;
            this.clientSocket = eClientSocket;
            this.eWrapper = wrapper;
            this.logger = logger;
        }

        public void TickPriceReceivedHandler(int tickerId, int fieldId, double price, int CanAutoExecute)
        {
            if (tickerId == requestId)
                {
                    switch (fieldId)
                    {
                        case IBGWTickType.DelayedBidPrice:
                            marketData.BidPrice = price;
                            break;
                        case IBGWTickType.DelayedAskPrice:
                            marketData.AskPrice = price;
                            break;
                        case IBGWTickType.DelayedLastPrice:
                            marketData.LatestPrice = price;
                            break;
                    }

                    if (marketData.LatestPrice != -10 && marketData.AskPrice != -10 && marketData.BidPrice != -10)
                    {
                        taskCompletionSource.SetResult(marketData);
                        clientSocket.cancelMktData(requestId);
                        Detach();
                    }
                }
        }

        public override void ErrorReceivedHandler(int id, int errorCode, string errorMsg, string advancedOrderRejectJson)
        {
            if (id == requestId)
            {
                if (errorCode == 10167)
                {
                    logger.LogWarn($"Id: {id} errorCode: {errorCode}. {errorMsg}. advancedOrderRejectJson: {advancedOrderRejectJson}");
                    return;
                }
                
                var errorMessage = $"Id: {id} errorCode: {errorCode}. {errorMsg}. advancedOrderRejectJson: {advancedOrderRejectJson}";
                logger.LogError($"[IBGWClient::MarketData::ErrorReceivedHandler] {errorMessage}");
                clientSocket.cancelMktData(requestId);
                
                taskCompletionSource.SetException(new Exception(errorMessage));
                Detach();
            }
        }

        protected override void Detach()
        {
            eWrapper.TickPriceReceived -= TickPriceReceivedHandler;
            eWrapper.ErrorReceived -= ErrorReceivedHandler;
        }
    }
}