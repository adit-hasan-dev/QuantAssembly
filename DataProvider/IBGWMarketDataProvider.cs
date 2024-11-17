using TradingBot.Impl.IBGW;
using TradingBot.Models;
using TradingBot.Logging;
using System.ComponentModel;

namespace TradingBot.DataProvider
{
    public class IBGWMarketDataProvider : IMarketDataProvider
    {
        private readonly IBGWClient ibgwClient;
        private readonly ILogger logger;
        private readonly Dictionary<string, int> tickerSymbolToIdMap = new Dictionary<string, int>();
        // tickerid -> marketdata
        private readonly Dictionary<int, MarketData> marketDataMap = new Dictionary<int, MarketData>();
        private readonly object lockObj = new object();

        private int NextRequestId = 0;

        public IBGWMarketDataProvider(IBGWClient ibgwClient, ILogger logger)
        {
            this.ibgwClient = ibgwClient;
            this.logger = logger;
            this.ibgwClient.TickPriceReceived += OnTickPriceReceived;
        }

        public void SubscribeMarketData(string ticker)
        {
            lock (lockObj)
            {
                if (!tickerSymbolToIdMap.TryGetValue(ticker, out int requestId))
                {
                    requestId = NextRequestId++;
                    tickerSymbolToIdMap[ticker] = requestId;
                    ibgwClient.RequestMarketData(ticker, requestId);
                }
            }
        }

        private void OnTickPriceReceived(int tickerId, int field, double price, int canAutoExecute)
        {
            lock (lockObj)
            {
                logger.LogDebug($"[IBGWMarketDataProvider] OnTickPriceReceived: Received tick price for tickerId: {tickerId}, field: {field}, price: {price}");
                if (tickerSymbolToIdMap.ContainsValue(tickerId))
                {
                    string symbol = tickerSymbolToIdMap.FirstOrDefault(kvp => kvp.Value == tickerId).Key;
                    if (!marketDataMap.TryGetValue(tickerId, out var marketData))
                    {
                        marketData = new MarketData { Symbol = symbol };
                        marketDataMap[tickerId] = marketData;
                    }

                    switch (field)
                    {
                        case IBGWTickType.DelayedBidPrice:
                            marketData.BidPrice = price;
                            logger.LogInfo($"Bid price updated for {symbol}: {price}");
                            break;
                        case IBGWTickType.DelayedAskPrice:
                            marketData.AskPrice = price;
                            logger.LogInfo($"Ask price updated for {symbol}: {price}");
                            break;
                        case IBGWTickType.DelayedLastPrice:
                            marketData.LatestPrice = price;
                            logger.LogInfo($"Last price updated for {symbol}: {price}");
                            break;
                            // Add more cases for other fields if needed
                    }
                }
            }
        }

        public double GetLatestPrice(string ticker)
        {
            // Start a market data request if not already done
            SubscribeMarketData(ticker);

            lock (lockObj)
            {
                if (tickerSymbolToIdMap.TryGetValue(ticker, out int tickerId))
                {
                    if (marketDataMap.TryGetValue(tickerId, out var marketData))
                    {
                        return marketData.LatestPrice;
                    }
                }

                throw new InvalidOperationException($"No market data available for ticker: {ticker}");
            }
        }

        public double GetRSI(string ticker)
        {
            // Implement logic to calculate RSI if available
            throw new NotImplementedException();
        }

        public (double MACD, double Signal) GetMACD(string ticker)
        {
            // Implement logic to calculate MACD and Signal if available
            throw new NotImplementedException();
        }

        public MarketData GetMarketData(string tickerSymbol)
        {
            // Start a market data request if not already done
            SubscribeMarketData(tickerSymbol);

            lock (lockObj)
            {
                if (tickerSymbolToIdMap.TryGetValue(tickerSymbol, out int tickerId))
                {
                    if (marketDataMap.TryGetValue(tickerId, out var marketData))
                    {
                        return marketData;
                    }
                }

                throw new InvalidOperationException($"No market data available for ticker: {tickerSymbol}");
            }
        }
    }

}