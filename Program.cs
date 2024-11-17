using QuantAssembly.Config;
using QuantAssembly.DataProvider;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Logging;
using QuantAssembly.Orchestratration;

namespace QuantAssembly
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IConfig config = new Config.Config();
            ILogger logger = new Logger("app.log", "transaction.log");
            logger.SetDebugToggle(config.EnableDebugLog);

            using (var client = new IBGWClient(logger))
            {
                // Connect to IB Gateway
                client.Connect("127.0.0.1", 4002, 0);

                var marketDataProvider = new IBGWMarketDataProvider(client, logger);
                var accountDataProvider = new IBGWAccountDataProvider(client,config.AccountId, logger);

                logger.LogInfo("Getting account data ...");
                var accountData = accountDataProvider.GetAccountData();
                logger.LogInfo(accountData.ToString());

                // Request market data for AAPL and MSFT
                logger.LogInfo("Requesting market data for AAPL and MSFT...");
                marketDataProvider.SubscribeMarketData("AAPL");
                marketDataProvider.SubscribeMarketData("MSFT");

                // Wait for some time to receive data updates
                Thread.Sleep(10000); // Wait for 10 seconds

                // Fetch and log the latest market data for AAPL
                var marketData = marketDataProvider.GetMarketData("AAPL");
                logger.LogInfo($"Latest bid price for AAPL: {marketData.BidPrice}");
                logger.LogInfo($"Latest ask price for AAPL: {marketData.AskPrice}");
                logger.LogInfo($"Latest price for AAPL: {marketData.LatestPrice}");

                // Fetch and log the latest market data for MSFT
                marketData = marketDataProvider.GetMarketData("MSFT");
                logger.LogInfo($"Latest bid price for MSFT: {marketData.BidPrice}");
                logger.LogInfo($"Latest ask price for MSFT: {marketData.AskPrice}");
                logger.LogInfo($"Latest price for MSFT: {marketData.LatestPrice}");

                logger.LogInfo("Testing Strategy Loader:");
                var orchestrator = new Orchestrator(logger);
                orchestrator.LoadStrategy("MSFT", "Strategy/TestStrategy.json");
            }
        }

    }
}
