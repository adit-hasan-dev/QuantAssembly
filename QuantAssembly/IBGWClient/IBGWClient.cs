using IBApi;
using QuantAssembly.Logging;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Models.Constants;
using QuantAssembly.Models;
//https://manga4life.com/read-online/Hanazono-Twins-chapter-11.html

namespace QuantAssembly.Impl.IBGW
{
    public class IBGWClient : IDisposable
    {
        private readonly ILogger logger;
        private EWrapperImpl eWrapperImpl;
        private EClientSocket eClientSocket;
        private EReaderSignal signal;
        private ManualResetEvent manualResetEvent;
        private EReader eReader;
        public bool IsConnected { get; private set; }
        private bool disposed;

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
        public void RequestMarketData(string ticker, int requestId)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to IB Gateway");

            var contract = new Contract
            {
                Symbol = ticker,
                SecType = "STK",
                Exchange = "SMART",
                Currency = "USD"
            };
            logger.LogInfo($"[IBGWClient] Requesting market data for {ticker}, request ID: {requestId}");
            eClientSocket.reqMktData(requestId, contract, "", false, false, null);
        }

        public void RequestAccountSummary()
        {
            if (!IsConnected) 
                throw new InvalidOperationException("Not connected to IB Gateway"); 
            
            // Request account summary 
            logger.LogInfo($"[IBGWClient] Requesting account summary");
            eClientSocket.reqAccountSummary(1, "All", "NetLiquidation,TotalCashValue,GrossPositionValue");
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