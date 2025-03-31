using IBApi;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Models;
using QuantAssembly.Common.Constants;
using QuantAssembly.Core.Models;

namespace QuantAssembly.Impl.IBGW
{
    public class IBGWClient : IIBGWClient, IDisposable
    {
        private readonly ILogger logger;
        private EWrapperImpl eWrapperImpl;
        private EClientSocket eClientSocket;
        private EReaderSignal signal;
        private ManualResetEvent manualResetEvent;
        private EReader eReader;
        public bool IsConnected { get; private set; }
        private bool disposed;
        private int nextValidRequestId = 0;


        public event Action<int, int, double, int> TickPriceReceived
        {
            add { eWrapperImpl.TickPriceReceived += value; }
            remove { eWrapperImpl.TickPriceReceived -= value; }
        }

        public event Action<string, string, string, string, string> AccountSummaryReceived
        {
            add { eWrapperImpl.AccountSummaryReceived += value; }
            remove { eWrapperImpl.AccountSummaryReceived -= value; }
        }


        public IBGWClient(ILogger logger)
        {
            this.eWrapperImpl = new EWrapperImpl(logger);
            this.signal = new EReaderMonitorSignal();
            this.eClientSocket = new EClientSocket(eWrapperImpl, signal);
            this.logger = logger;
            this.eWrapperImpl.clientSocket = this.eClientSocket;

            // Connect to IB Gateway
            Connect("127.0.0.1", 4002, 0);
        }

        public void Connect(string host, int port, int clientId, MarketDataType dataType = MarketDataType.Delayed)
        {
            eClientSocket.eConnect(host, port, clientId);
            eReader = new EReader(eClientSocket, signal);
            eReader.Start();
            new Thread(() =>
            {
                while (eClientSocket.IsConnected())
                {
                    signal.waitForSignal();
                    eReader.processMsgs();
                }
            })
            { IsBackground = true }.Start();

            eWrapperImpl.ManualResetEvent.Reset();
            eClientSocket.reqMarketDataType(dataType == MarketDataType.RealTime ? 1 : 3);

            while (eWrapperImpl.NextOrderId <= 0)
            {
                Thread.Sleep(10);
            }

            IsConnected = true;
        }

        // TODO: Make this a generic method for all financial instruments
        public async Task<MarketData> RequestMarketDataAsync(string ticker, string instrumentType = "STK", string currency = "USD")
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to IB Gateway");

            int requestId = nextValidRequestId++;
            logger.LogInfo($"[IBGWClient::RequestMarketDataAsync] Requesting market data for {ticker}, request ID: {requestId}");

            var tcs = new TaskCompletionSource<MarketData>();
            var marketData = new MarketData
            {
                Symbol = ticker,
                BidPrice = -10,
                LatestPrice = -10,
                AskPrice = -10
            };

            MarketDataEventHandler eventHandler = new MarketDataEventHandler(
                requestId,
                marketData,
                tcs,
                eWrapperImpl,
                eClientSocket,
                logger);

            eWrapperImpl.TickPriceReceived += eventHandler.TickPriceReceivedHandler;
            eWrapperImpl.ErrorReceived += eventHandler.ErrorReceivedHandler;

            var contract = new Contract
            {
                Symbol = ticker,
                SecType = instrumentType,
                Exchange = "SMART",
                Currency = currency,
            };
            eClientSocket.reqMktData(requestId, contract, "", false, false, null);

            return await tcs.Task;
        }

        public async Task<AccountData> RequestAccountSummaryAsync(string accountId)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to IB Gateway");

            int requestId = nextValidRequestId++;

            var tcs = new TaskCompletionSource<AccountData>();
            var accountData = new AccountData
            {
                AccountID = accountId,
                TotalPortfolioValue = -10,
                Liquidity = -10,
                Equity = -10
            };

            AccountDataEventHandler eventHandler = new AccountDataEventHandler(
                requestId,
                accountData,
                tcs,
                eWrapperImpl,
                eClientSocket,
                logger);

            eWrapperImpl.AccountSummaryReceived += eventHandler.AccountSummaryReceivedHandler;
            eWrapperImpl.ErrorReceived += eventHandler.ErrorReceivedHandler;
            logger.LogInfo($"[IBGWClient] Requesting account summary");
            eClientSocket.reqAccountSummary(requestId, "All", "NetLiquidation,TotalCashValue,GrossPositionValue");

            return await tcs.Task;
        }

        public async Task<TransactionResult> PlaceOrder(Position position, OrderType orderType, OrderAction action)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to IB Gateway");

            var tcs = new TaskCompletionSource<TransactionResult>();

            var ibOrderId = eWrapperImpl.NextOrderId++;
            position.PlatformOrderId = ibOrderId.ToString();

            var contract = new Contract
            {
                Symbol = position.Symbol,
                SecType = position.InstrumentType == InstrumentType.Stock ? "STK" : "Error",
                Exchange = "SMART",
                Currency = position.Currency,
            };
            var order = GenerateOrder(position, orderType, action);

            var result = new TransactionResult
            {
                OrderId = ibOrderId.ToString(),
                TransactionState = TransactionState.Pending
            };

            OrderEventHandler eventHandler = new OrderEventHandler(
                ibOrderId,
                position,
                logger,
                result,
                tcs,
                eWrapperImpl,
                eClientSocket
            );

            eWrapperImpl.ErrorReceived += eventHandler.ErrorReceivedHandler;
            eWrapperImpl.OrderStatusReceived += eventHandler.OrderStatusHandler;
            eWrapperImpl.ExecDetailsReceived += eventHandler.ExecDetailsHandler;

            logger.LogInfo($"[IBGWClient::PlaceOrder] Placing order to {action} with type: {orderType} for position:\n {position}");
            eClientSocket.placeOrder(ibOrderId, contract, order);
            return await tcs.Task;
        }

        public async Task<ContractDetails> GetSymbolContractDetailsAsync(string symbol, InstrumentType instrumentType = InstrumentType.Stock, string currency = "USD")
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to IB Gateway");

            int requestId = nextValidRequestId++;

            var tcs = new TaskCompletionSource<ContractDetails>();
            var contract = GenerateSymbolContract(symbol, instrumentType, currency);
            var handler = new ContractDetailsEventHandler(symbol, requestId, tcs, eWrapperImpl, eClientSocket, logger);
            eWrapperImpl.ContractDetailsReceived += handler.ContractDetailsReceivedHandler;
            eWrapperImpl.ErrorReceived += handler.ErrorReceivedHandler;

            logger.LogInfo($"Requesting contract details for symbol: {symbol}");
            eClientSocket.reqContractDetails(requestId, contract);
            return await tcs.Task;
        }

        private Contract GenerateSymbolContract(string symbol, InstrumentType instrumentType, string currency = "USD")
        {
            // TODO: Support mutliple intrument types
            return new Contract
            {
                Symbol = symbol,
                SecType = instrumentType == InstrumentType.Stock ? "STK" : "Error",
                Exchange = "SMART",
                Currency = currency,
            };
        }

        private Order GenerateOrder(Position position, OrderType orderType, OrderAction action)
        {
            var order = new Order
            {
                Action = action == OrderAction.Buy ? "BUY" : "SELL",
                TotalQuantity = position.Quantity,
                OrderRef = position.PositionGuid.ToString()
            };
            switch (orderType)
            {
                case OrderType.Market:
                    order.OrderType = "MKT";
                    break;
                case OrderType.Limit:
                    order.OrderType = "LMT";
                    order.LmtPrice = position.CurrentPrice;
                    break;
                default:
                    throw new NotImplementedException("Only market orders and stop limit order types are supported for now");
            }
            return order;
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                eClientSocket.eDisconnect();
                IsConnected = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (IsConnected)
                {
                    Disconnect();
                }
                manualResetEvent?.Dispose();
            }

            disposed = true;
        }

        ~IBGWClient()
        {
            Dispose(false);
        }
    }

}