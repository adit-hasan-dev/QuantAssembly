using IBApi;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Logging;
using QuantAssembly.Models;
using QuantAssembly.Models.Constants;

namespace QuantAssembly.Tests
{
    public class MockIBGWClient : IIBGWClient
    {
        public ContractDetails contractDetails{ get; set; }
        public async Task<TransactionResult> PlaceOrder(Position position, OrderType orderType, OrderAction action)
        {
            // Simulate the behavior of placing an order
            var tcs = new TaskCompletionSource<TransactionResult>();
            int ibOrderId = 1;

            position.PlatformOrderId = ibOrderId.ToString();

            var result = new TransactionResult
            {
                OrderId = ibOrderId.ToString(),
                TransactionState = TransactionState.Pending
            };

            // Simulate side effects
            result.TransactionState = action switch
            {
                OrderAction.Buy => TransactionState.Completed,
                OrderAction.Sell => TransactionState.Completed,
                _ => TransactionState.Failed
            };

            if (result.TransactionState == TransactionState.Completed)
            {
                position.OpenPrice = action == OrderAction.Buy ? 100 : position.OpenPrice; // Simulate a price
                position.CurrentPrice = position.OpenPrice;
                position.OpenTime = DateTime.Now;
                if (action == OrderAction.Sell)
                {
                    position.ClosePrice = position.CurrentPrice;
                    position.CloseTime = DateTime.Now;
                    position.State = PositionState.Closed;
                }
            }

            tcs.SetResult(result);

            return await tcs.Task;
        }

        // Stub the other methods to avoid actual external calls
        public Task<MarketData> RequestMarketDataAsync(string ticker, int requestId, string instrumentType = "STK", string currency = "USD")
        {
            throw new NotImplementedException();
        }

        public Task<AccountData> RequestAccountSummaryAsync(string accountId)
        {
            throw new NotImplementedException();
        }

        public async Task<ContractDetails> GetSymbolContractDetailsAsync(string symbol, InstrumentType instrumentType = InstrumentType.Stock, string currency = "USD")
        {
            return contractDetails;
        }
    }

}