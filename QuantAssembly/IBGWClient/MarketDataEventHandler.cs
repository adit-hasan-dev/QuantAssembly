using IBApi;
using QuantAssembly.Models;

namespace QuantAssembly.Impl.IBGW
{
    internal class MarketDataEventHandler : BaseEventHandler<MarketData>
    {
        public int requestId { get; set; }
        public MarketData marketData { get; set; }

        public MarketDataEventHandler(
            int requestId, 
            MarketData marketData,
            TaskCompletionSource<MarketData> taskCompletionSource,
            EWrapperImpl wrapper,
            EClientSocket eClientSocket)
        {
            this.requestId = requestId;
            this.marketData = marketData;
            this.taskCompletionSource = taskCompletionSource;
            this.clientSocket = eClientSocket;
            this.eWrapper = wrapper;
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
                        eWrapper.TickPriceReceived -= TickPriceReceivedHandler;
                        clientSocket.cancelMktData(requestId);
                    }
                }
        }
    }
}