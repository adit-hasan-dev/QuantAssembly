namespace QuantAssembly.Impl.IBGW
{
    using System;
    using System.Collections.Generic;
    using IBApi;
    using QuantAssembly.Logging;
    using QuantAssembly.Models.Constants;

    public class EWrapperImpl : EWrapper
    {
        private readonly ILogger logger;
        private const int BidFieldId = 66;
        private const int AskFieldId = 67;
        private const int LastFieldId = 68;
        public EClientSocket clientSocket { get; set; }
        public ManualResetEvent ManualResetEvent { get; private set; } = new ManualResetEvent(false);

        // Callback Events
        public event Action<int, int, double, int> TickPriceReceived;

        public EWrapperImpl(ILogger _logger)
        {
            this.logger = _logger;
        }

        public string BboExchange { get; private set; }

        public int NextOrderId { get; set; }

        public void tickPrice(int tickerId, int field, double price, TickAttrib attribs) 
        { 
            TickPriceReceived?.Invoke(tickerId, field, price, attribs.CanAutoExecute ? 1 : 0); 
        }

        public void nextValidId(int orderId)
        {
            NextOrderId = orderId + 1;
        }

        public void connectAck()
        {
            if (clientSocket.AsyncEConnect)
                clientSocket.startApi();
            logger.LogInfo("Connection acknowledged");
        }

        public void marketDataType(int reqId, int marketDataType)
        {
            logger.LogDebug($"MarketDataType: {marketDataType} ReqId: {reqId}");
        }

        public void tickSize(int tickerId, int field, decimal size)
        {
            logger.LogDebug($"Tick size: TickerId: {tickerId} Field: {field} Size: {size}");
        }

        public void tickString(int tickerId, int field, string value)
        {
            logger.LogDebug($"Tick string: TickerId: {tickerId} Field: {field} Value: {value}");
        }

        // Start - AccountData
        public event Action<string, string, string, string, string> AccountSummaryReceived;

        public void accountSummary(int reqId, string account, string tag, string value, string currency) 
        { 
            logger.LogDebug($"Account Summary: ReqId={reqId}, Account={account}, Tag={tag}, Value={value}, Currency={currency}"); 
            AccountSummaryReceived?.Invoke(reqId.ToString(), account, tag, value, currency); 
        } 
            
        public void accountSummaryEnd(int reqId) 
        { 
            logger.LogDebug($"Account Summary End: ReqId={reqId}"); 
        } 
        
        public void accountDownloadEnd(string account) 
        { 
            logger.LogDebug($"Account Download End: {account}");
        }

        // End - AccountData 

        public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
        {
            throw new NotImplementedException();
        }

        public void accountUpdateMultiEnd(int requestId)
        {
            throw new NotImplementedException();
        }

        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            throw new NotImplementedException();
        }

        public void commissionReport(CommissionReport commissionReport)
        {
            throw new NotImplementedException();
        }

        public void completedOrder(Contract contract, Order order, OrderState orderState)
        {
            throw new NotImplementedException();
        }

        public void completedOrdersEnd()
        {
            throw new NotImplementedException();
        }

        public void connectionClosed()
        {
            logger.LogInfo("Connection Closed");
        }

        public event Action<int, ContractDetails> ContractDetailsReceived;

        public void contractDetails(int reqId, ContractDetails contractDetails)
        {
            logger.LogDebug($"Contract details received for reqId {reqId}\n{contractDetails}");
            ContractDetailsReceived?.Invoke(reqId, contractDetails);
        }

        // No-op
        public void contractDetailsEnd(int reqId)
        {
            return;
        }

        public void currentTime(long time)
        {
            throw new NotImplementedException();
        }

        public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
        {
            throw new NotImplementedException();
        }

        public void displayGroupList(int reqId, string groups)
        {
            throw new NotImplementedException();
        }

        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            throw new NotImplementedException();
        }

        public void error(Exception e)
        {
            logger.LogError(e);
        }

        public void error(string str)
        {
            logger.LogError(str);
        }

        public event Action<int, int, string, string> ErrorReceived;

        public void error(int id, int errorCode, string errorMsg, string advancedOrderRejectJson)
        {
            if (IBGWErrorCodes.NotificationErrorCodes.Contains(errorCode))
            {
                logger.LogDebug($"Id: {id}, ErrorCode: {errorCode}, ErrorMessage: {errorMsg}");
            }
            else
            {
                logger.LogError($"Id: {id}, ErrorCode: {errorCode}, ErrorMessage: {errorMsg}, AdvancedOrderRejection: {advancedOrderRejectJson}");
            }
            ErrorReceived?.Invoke(id, errorCode, errorMsg, advancedOrderRejectJson);
        }

        public event Action<int, Contract, Execution> ExecDetailsReceived;
        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            var debugMessage = "ExecDetails. " + reqId + " - " + contract.Symbol + ", " + contract.SecType+", " + contract.Currency+" - " + execution.ExecId + ", " + Util.IntMaxString(execution.OrderId) + 
                ", " + Util.DecimalMaxString(execution.Shares) + ", " + Util.DecimalMaxString(execution.CumQty) + ", " + execution.LastLiquidity + ", " + execution.Price;
            logger.LogDebug($"[EWrapperImpl::execDetails] Exec details received: {debugMessage}");
            ExecDetailsReceived?.Invoke(reqId, contract, execution);
        }

        public void execDetailsEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void familyCodes(FamilyCode[] familyCodes)
        {
            throw new NotImplementedException();
        }

        public void fundamentalData(int reqId, string data)
        {
            throw new NotImplementedException();
        }

        public void headTimestamp(int reqId, string headTimestamp)
        {
            throw new NotImplementedException();
        }

        public void histogramData(int reqId, HistogramEntry[] data)
        {
            throw new NotImplementedException();
        }

        public void historicalData(int reqId, Bar bar)
        {
            throw new NotImplementedException();
        }

        public void historicalDataEnd(int reqId, string start, string end)
        {
            throw new NotImplementedException();
        }

        public void historicalDataUpdate(int reqId, Bar bar)
        {
            throw new NotImplementedException();
        }

        public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
        {
            throw new NotImplementedException();
        }

        public void historicalNewsEnd(int requestId, bool hasMore)
        {
            throw new NotImplementedException();
        }

        public void historicalSchedule(int reqId, string startDateTime, string endDateTime, string timeZone, HistoricalSession[] sessions)
        {
            throw new NotImplementedException();
        }

        public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
        {
            throw new NotImplementedException();
        }

        public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
        {
            throw new NotImplementedException();
        }

        public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
        {
            throw new NotImplementedException();
        }

        public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            throw new NotImplementedException();
        }

        public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
        {
            throw new NotImplementedException();
        }

        public void newsArticle(int requestId, int articleType, string articleText)
        {
            throw new NotImplementedException();
        }

        public void newsProviders(NewsProvider[] newsProviders)
        {
            throw new NotImplementedException();
        }

        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            throw new NotImplementedException();
        }

        public void openOrderEnd()
        {
            throw new NotImplementedException();
        }

        public void managedAccounts(string accountsList)
        {
            logger.LogInfo($"Accounts list: {accountsList}");
        }

        public void orderBound(long orderId, int apiClientId, int apiOrderId)
        {
            throw new NotImplementedException();
        }

        public event Action<int, string, decimal, decimal, double, int, int, double, int, string, double> OrderStatusReceived;
        public void orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            string message = "OrderStatus. Id: " + orderId + ", Status: " + status + ", Filled: " + Util.DecimalMaxString(filled) + ", Remaining: " + Util.DecimalMaxString(remaining)
                + ", AvgFillPrice: " + Util.DoubleMaxString(avgFillPrice) + ", PermId: " + Util.IntMaxString(permId) + ", ParentId: " + Util.IntMaxString(parentId) + 
                ", LastFillPrice: " + Util.DoubleMaxString(lastFillPrice) + ", ClientId: " + Util.IntMaxString(clientId) + ", WhyHeld: " + whyHeld + ", MktCapPrice: " + Util.DoubleMaxString(mktCapPrice);
            logger.LogDebug(message);
            OrderStatusReceived?.Invoke(orderId, status, filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld, mktCapPrice);
        }

        public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
        {
            throw new NotImplementedException();
        }

        public void pnlSingle(int reqId, decimal pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
        {
            throw new NotImplementedException();
        }

        public void position(string account, Contract contract, decimal pos, double avgCost)
        {
            throw new NotImplementedException();
        }

        public void positionEnd()
        {
            throw new NotImplementedException();
        }

        public void positionMulti(int requestId, string account, string modelCode, Contract contract, decimal pos, double avgCost)
        {
            throw new NotImplementedException();
        }

        public void positionMultiEnd(int requestId)
        {
            throw new NotImplementedException();
        }

        public void realtimeBar(int reqId, long date, double open, double high, double low, double close, decimal volume, decimal WAP, int count)
        {
            throw new NotImplementedException();
        }

        public void receiveFA(int faDataType, string faXmlData)
        {
            throw new NotImplementedException();
        }

        public void replaceFAEnd(int reqId, string text)
        {
            throw new NotImplementedException();
        }

        public void rerouteMktDataReq(int reqId, int conId, string exchange)
        {
            throw new NotImplementedException();
        }

        public void rerouteMktDepthReq(int reqId, int conId, string exchange)
        {
            throw new NotImplementedException();
        }

        public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            throw new NotImplementedException();
        }

        public void scannerDataEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void scannerParameters(string xml)
        {
            throw new NotImplementedException();
        }

        public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            throw new NotImplementedException();
        }

        public void securityDefinitionOptionParameterEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
        {
            throw new NotImplementedException();
        }

        public void softDollarTiers(int reqId, SoftDollarTier[] tiers)
        {
            throw new NotImplementedException();
        }

        public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
        {
            throw new NotImplementedException();
        }

        public void tickByTickAllLast(int reqId, int tickType, long time, double price, decimal size, TickAttribLast tickAttribLast, string exchange, string specialConditions)
        {
            throw new NotImplementedException();
        }

        public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, decimal bidSize, decimal askSize, TickAttribBidAsk tickAttribBidAsk)
        {
            throw new NotImplementedException();
        }

        public void tickByTickMidPoint(int reqId, long time, double midPoint)
        {
            throw new NotImplementedException();
        }

        public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
        {
            throw new NotImplementedException();
        }

        public void tickGeneric(int tickerId, int field, double value)
        {
            throw new NotImplementedException();
        }

        public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
        {
            throw new NotImplementedException();
        }

        public void tickOptionComputation(int tickerId, int field, int tickAttrib, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            throw new NotImplementedException();
        }
        public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
        {
            logger.LogInfo($"id={tickerId} minTick = {Util.DoubleMaxString(minTick)} bboExchange = {bboExchange} snapshotPermissions = {Util.IntMaxString(snapshotPermissions)}");
            BboExchange = bboExchange;
        }

        public void tickSnapshotEnd(int tickerId)
        {
            throw new NotImplementedException();
        }

        public void updateAccountTime(string timestamp)
        {
            throw new NotImplementedException();
        }

        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, decimal size)
        {
            throw new NotImplementedException();
        }

        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, decimal size, bool isSmartDepth)
        {
            throw new NotImplementedException();
        }

        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            throw new NotImplementedException();
        }

        public void updatePortfolio(Contract contract, decimal position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
        {
            throw new NotImplementedException();
        }

        public void userInfo(int reqId, string whiteBrandingId)
        {
            throw new NotImplementedException();
        }

        public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }

        public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            throw new NotImplementedException();
        }

        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }

        public void verifyMessageAPI(string apiData)
        {
            throw new NotImplementedException();
        }

        public void wshEventData(int reqId, string dataJson)
        {
            throw new NotImplementedException();
        }

        public void wshMetaData(int reqId, string dataJson)
        {
            throw new NotImplementedException();
        }

        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            throw new NotImplementedException();
        }
    }
}