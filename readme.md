# Quant Assembly

## Overview
Quant Assembly is a modular trading bot application built in dotnet designed for developers to seamlessly define, test, and execute trading strategies. This project, which is a work in progress, serves as an exploration into the realm of quantitative finance and algorithmic trading.

Despite the pretentious name, the current version supports straightforward condition-based strategies, though its architecture is built to accommodate more sophisticated quantitative strategies in the future, depending on evolving interests and needs. The application presently integrates with the Interactive Brokers (IB) Gateway API to retrieve market data and execute trades, but it is designed for easy extension to other data providers and execution platforms.

## Architecture
The architecture of the trading bot is designed with flexibility and extensibility in mind. Here are the main components:

- **Quant**: This is the orchestrator that brings all subsystems together and carries out the main logic of the application. You can find the main loop here and also the setup code that chooses the implementations for each component. This class is used as the entry point to start the application:

```csharp
public static void Main(string[] args)
{
    var quant = new Quant();

    Console.CancelKeyPress += (sender, e) => {
        e.Cancel = true;
        quant.Terminate();
    };
    quant.Run();
}
```
- **IMarketDataProvider**: Interface for retrieving real time market data like the latest price quotes, volume etc. Currently there's an implementation based on the IBKR Gateway API called `IBGWMarketDataProvider`. 

- **IHistoricalMarketDataProvider**: Interface for retrieving historical market data and indicators like historical prices, RSI, EMA etc. There are currently two implementations:
  - **StockIndicatorsHistoricalDataProvider**: This implementation is based the [Stock Indicators for .NET](https://github.com/DaveSkender/Stock.Indicators) library to compute the metrics and [Alpaca Markets API](https://docs.alpaca.markets/) to retrieve historical data. This is the recommended implementation to use since it has the fewest limitations as a free option and is quite easy to use.
  - **AlphaVantageHistoricalMarketDataProvider**: This implementation is based on the [Alpha Vantage API](https://www.alphavantage.co/documentation/) and only uses the free endpoints. The free version of AlphaVantage is very limited (e.g. only 25 calls/day) so unless you are willing to pay for the premium API, this remains the inferior choice.

- **IAccountDataProvider**: Interface for retrieving account and portfolio information. Currently there is one implementation based on IB Gateway, since that is the main platform supported for the application, with trades being executed through the same platform.

- **IStrategyProcessor**: Interface responsible for loading and evaluating trading strategies. It uses the data from the data providers and the defined strategies to produce entry and exit signals. 
 
- **IRiskManager**: Interface responsible for managing trade risks and position sizing. It takes into account available funds in the portfolio, configured global stop loss, max draw down percentage etc and determines if an entry signal produced by a strategy can actually be used to open a position. Currently only supports long positions for stocks. There is a very simple implementation of this that uses account value percentage to size positions. 
  
- **ITradeManager**: Interface responsible for carrying out trades based on the parameters set by the risk manager. The only implementation is based on the IB Gateway API.
- **ILedger**: All open and closed positions are recorded in the Ledger. At launch the ledger is loaded onto memory and every transaction is added to it to be saved to disk. The built-in implementation is a json file with all positions recorded.


### How Strategies Work
Strategies are defined using simple, human-readable JSON files. Each strategy can include conditions for entry, exit, stop loss, and take profit. Conditions can be combined using logical operators (AND/OR) within each group. You can add your strategy definitions to the Strategy directory to automatically copy it to the build directory, or you can add build tasks to copy strategies from other locations to the build directory.

Note: Currently either AND or OR operators are supported within a single condition group, not both.

#### Example Strategy Definition (JSON)
```json
{
    "Name": "MACD_RSI_Strategy",
    "State": "Active",
    "EntryConditions": {
        "LogicalOperator": "AND",
        "Conditions": [
            {
                "LeftHandOperand": "MACD",
                "Operator": "GreaterThan",
                "RightHandOperand": "SignalLine"
            },
            {
                "Property": "RSI",
                "Operator": "LessThan",
                "Value": 70
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
                "Property": "LossPercentage",
                "Operator": "GreaterThan",
                "Value": 5
            }
        ]
    },
    "TakeProfitConditions": {
        "LogicalOperator": "AND",
        "Conditions": [
            {
                "Property": "ProfitPercentage",
                "Operator": "GreaterThan",
                "Value": 10
            }
        ]
    }
}
```

### Using the Project
To get started with your own trading bot implementation, follow these steps:

1. **Clone the Repository**: Clone the project repository from GitHub: `git clone https://github.com/adit-hasan-dev/QuantAssembly.git`.
2. **Install dotnet**: Download and install [the dotnet SDK](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-9.0.101-windows-x64-installer).
3. **Install Dependencies**: Navigate to the solution directory and pull all dependencies using `dotnet restore`.
4. **Install IB Gateway**: Follow the steps below to install IB Gateway.
   - **Create an IBKR Account**: If you don't already have an account, you need to create one. Follow the instructions on the [IBKR website](https://www.interactivebrokers.com/en/index.php?f=46345).
   - **Download and Install IB Gateway**: Download the IB Gateway application from the [IBKR Software](https://www.interactivebrokers.com/en/index.php?f=5041) page. Follow the installation instructions provided.
5. **Configure IB Gateway**:
   - **Add Account ID**: Add your IBKR account ID to the `appsettings.json` file in the root of your project. And the relevant keys to the config (configured to be `appsettings.json` by default).
6. **Define Strategies**: Create your trading strategies in JSON format and store them in a directory.
7. **Load strategies and securities**: Use the `TickerStrategyMap` in appsettings.json to define which tickers to monitor and the corresponding strategy to use on them. For example:
```json
"TickerStrategyMap": {
    "SPY": "strategies/SPY_strategy.json",
    "AAPL": "strategies/AAPL_strategy.json",
    "GOOGL": "strategies/GOOGL_strategy.json"
    }
```
To avoid conflicting signals, a ticker can only be mapped to a single strategy.

8. **Launch the IB Gateway** application before launching the application.

Optionally, you can implement `IMarketDataProvider`, `IHistoricalMarketDataProvider`, `IAccountDataProvider`, `IRiskManager`  and/or `ITradeManager` to use other platforms, or a mixture of several.

### Configuration

The application uses configurations defined in the `appsettings.json` to store parameters that often require tweaking. Settings that only apply to a specific implementation are stored in the `CustomProperties` field. An example appsettings.json file:
```json
{
    "AccountId": "<account.No>",
    "EnableDebugLog": "true",
    "LogFilePath": "app.log",
    "LedgerFilePath": "ledger.log",
    "PollingIntervalInMs": "30000",
    "TickerStrategyMap": {
        "SPY": "strategies/SPY_strategy.json",
        "AAPL": "strategies/AAPL_strategy.json",
        "GOOGL": "strategies/GOOGL_strategy.json"
    },
    "APIKey": "<api_key>",
    "RiskManagement": {
        "GlobalStopLoss": 0.5,
        "MaxDrawDownPercentage": 0.6
    },
    "CustomProperties": {
        "AlpacaMarketsClientConfig": {
            "apiKey": "<key>",
            "apiSecret": "<secret>"
        }
    }
}
```

## Future Improvements
1. Back-testing framework
2. Support for defining complex quant strategies using ML, news feeds (maybe we can shove an LLM in here as well? Because why not?)
3. Support for options and futures
4. Multi-leg trade strategies
5. More sophisticated risk management functionality

## Conclusion
Quant Assembly is an evolving trading bot aimed at delving into the intricacies of quantitative finance. Although it is still under development, it offers a robust framework for building and testing trading strategies. This project not only serves as a valuable learning tool but also provides a solid foundation for those looking to create more advanced trading applications. Hopefully, it will inspire and assist others in their journey through quantitative finance and algorithmic trading. 

Happy trading! 📈🚀
