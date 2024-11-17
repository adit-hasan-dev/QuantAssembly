# Trading Bot

## Overview
This personal project is a modular trading bot application designed to allow developers to define, test, and execute trading strategies with ease. The current implementation supports integration with the Interactive Brokers (IB) Gateway API for retrieving market data and executing trades, but it can be extended to support other data providers and execution platforms.

## Architecture
The architecture of the trading bot is designed with flexibility and extensibility in mind. Here are the main components:

- **IMarketDataProvider**: Interface for retrieving market data.
- **IAccountDataProvider**: Interface for retrieving account information.
- **IExecutor**: Interface responsible for carrying out trades.
- **IBGWMarketDataProvider**: Implementation of `IMarketDataProvider` using the IB Gateway API.
- **IBGWAccountDataProvider**: Implementation of `IAccountDataProvider` using the IB Gateway API.
- **Orchestrator**: Class responsible for loading and evaluating trading strategies.

### How Strategies Work
Strategies are defined using simple, human-readable JSON files. Each strategy can include conditions for entry, exit, stop loss, and take profit. Conditions can be combined using logical operators (AND/OR) within each group. You can add your strategy definitions to the Strategy directory to automatically copy it to the build directory, or you can add build tasks to copy strategies from other locations to the build directory.

Note: Currently either AND or OR operators are supported within a single condition group, not both.

#### Example Strategy Definition (JSON)
```json
{
    "EntryConditions": {
        "LogicalOperator": "AND",
        "Conditions": [
            {
                "Property": "LatestPrice",
                "Operator": "LessThan",
                "Value": 50
            },
            {
                "Property": "RSI",
                "Operator": "LessThan",
                "Value": 30
            }
        ]
    },
    "ExitConditions": {
        "LogicalOperator": "OR",
        "Conditions": [
            {
                "Property": "RSI",
                "Operator": "GreaterThan",
                "Value": 70
            }
        ]
    },
    "StopLossConditions": {
        "LogicalOperator": "AND",
        "Conditions": [
            {
                "Property": "PercentageLoss",
                "Operator": "GreaterThan",
                "Value": 30
            }
        ]
    },
    "TakeProfitConditions": {
        "LogicalOperator": "AND",
        "Conditions": [
            {
                "Property": "PercentageGain",
                "Operator": "GreaterThanOrEqual",
                "Value": 30
            }
        ]
    }
}
```

### Using the Project
To get started with your own trading bot implementation, follow these steps:

1. **Clone the Repository**: Clone the project repository from GitHub.
2. **Install Dependencies**: Ensure you have the required dependencies, including `Newtonsoft.Json` for JSON parsing.
3. **Define Strategies**: Create your trading strategies in JSON format and store them in a directory.
4. **Load and Evaluate Strategies**: Use the `StrategyOrchestrator` to load and evaluate strategies.

Optionally, you can implement `IMarketDataProvider`, `IAccountDataProvider` and `IExecutor` to use other platforms, or a mixture of several.

#### Example Code
```csharp
using TradingBot.Config;
using TradingBot.DataProvider;
using TradingBot.Impl.IBGW;
using TradingBot.Logging;
using TradingBot.Orchestratration;

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
            var accountDataProvider = new IBGWAccountDataProvider(client, config.AccountId, logger);

            // Get account data
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

            // Load and evaluate strategy for MSFT
            var orchestrator = new Orchestrator(logger);
            orchestrator.LoadStrategy("MSFT", "Strategy/TestStrategy.json");

            // Example of evaluating strategy conditions
            var shouldOpen = orchestrator.ShouldOpen(marketData, accountData, "MSFT");
            logger.LogInfo($"Should open position for MSFT: {shouldOpen}");

            var shouldClose = orchestrator.ShouldClose(marketData, accountData, "MSFT");
            logger.LogInfo($"Should close position for MSFT: {shouldClose}");

            // Disconnect from IB Gateway
            client.Disconnect();
        }
    }
}
```

## Conclusion
This trading bot is still a work in progress. It is meant to provide a flexible and extensible framework for implementing automated trading strategies. The project includes an implementation for the IB Gateway API, but it should be straighforward to add implementations for other platforms. 

Happy trading! 📈🚀
