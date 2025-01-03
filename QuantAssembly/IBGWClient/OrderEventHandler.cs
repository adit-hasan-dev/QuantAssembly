using System.Xml.Serialization;
using IBApi;
using QuantAssembly.Logging;
using QuantAssembly.Models;

namespace QuantAssembly.Impl.IBGW
{
    internal class OrderEventHandler : BaseEventHandler<TransactionResult>
    {
        private readonly ILogger _logger;
        private TransactionResult result;
        private int ibOrderId;
        private Position position;
        public OrderEventHandler(
            int orderId,
            Position position,
            ILogger logger,
            TransactionResult transactionResult,
            TaskCompletionSource<TransactionResult> taskCompletionSource,
            EWrapperImpl wrapper,
            EClientSocket eClientSocket)
        {
            _logger = logger;
            result = transactionResult;
            this.taskCompletionSource = taskCompletionSource;
            this.eWrapper = wrapper;
            this.clientSocket = eClientSocket;
            this.ibOrderId = orderId;
            this.position = position;
        }

        public override void ErrorReceivedHandler(int id, int errorCode, string errorMsg, string advancedOrderRejectJson)
        {
            if (id == ibOrderId)
            {
                _logger.LogError($"[IBGWClient::OrderEventHandler::ErrorHandler] Id: {id} errorCode: {errorCode}. {errorMsg}. advancedOrderRejectJson: {advancedOrderRejectJson}");
                result.TransactionState = TransactionState.Failed;
                taskCompletionSource.SetResult(result);
                Detach();
            }
        }

        public void OrderStatusHandler(
                int orderId,
                string status,
                decimal filled,
                decimal remaining,
                double avgFillPrice,
                int permId,
                int parentId,
                double lastFillPrice,
                int clientId,
                string whyHeld,
                double mktCapPrice)
        {
            if (ibOrderId == orderId)
            {
                result.TransactionState = status switch
                {
                    _ when status.Equals(IBGWOrderStatus.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase) ||
                           status.Equals(IBGWOrderStatus.ApiCancelled.ToString(), StringComparison.OrdinalIgnoreCase) => TransactionState.Cancelled,
                    _ when status.Equals(IBGWOrderStatus.Filled.ToString(), StringComparison.OrdinalIgnoreCase) => TransactionState.Completed,
                    _ when !status.Equals(IBGWOrderStatus.Inactive.ToString(), StringComparison.OrdinalIgnoreCase) => TransactionState.Failed,
                    _ => result.TransactionState // Keep existing state for other statuses
                };

                if (result.TransactionState == TransactionState.Failed || result.TransactionState == TransactionState.Cancelled)
                {
                    taskCompletionSource.SetResult(result);
                    Detach();
                }
                if (result.TransactionState == TransactionState.Completed && remaining == 0)
                {
                    position.OpenPrice = avgFillPrice;
                    position.CurrentPrice = avgFillPrice;
                    position.OpenTime = DateTime.Now;
                    taskCompletionSource.SetResult(result);
                    Detach();
                }
            }
        }

        public void ExecDetailsHandler(int reqId, Contract contract, Execution execution)
        {
            // TODO: Handle partial fills
            if (execution.OrderId == ibOrderId ||
                execution.OrderRef.Equals(position.PositionGuid.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                position.OpenPrice = execution.Price;
                position.CurrentPrice = execution.Price;
                position.OpenTime = DateTime.Now;

                if ((int)execution.Shares == position.Quantity)
                {
                    taskCompletionSource.SetResult(result);
                    Detach();
                }
            }
        }

        protected override void Detach()
        {
            eWrapper.ExecDetailsReceived -= ExecDetailsHandler;
            eWrapper.OrderStatusReceived -= OrderStatusHandler;
            eWrapper.ErrorReceived -= ErrorReceivedHandler;
        }
    }
}