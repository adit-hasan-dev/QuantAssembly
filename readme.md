# Quant Assembly

## Overview
Quant Assembly is a modular trading bot framework built in dotnet designed for developers to seamlessly define, test, and execute trading strategies. This project, which is a work in progress, serves as an exploration into the realm of quantitative finance and algorithmic trading.

Quant Assembly consists of 3 different applications, in 3 projects:
1. [**QuantAssembly**](QuantAssembly/readme.md): This is the main trading application. It is a console application.
2. [**QuantAssembly.BackTest**](QuantAssembly.BackTest/readme.md): This is a backtesting application using the main QuantAsembly components to run simulations.
3. [**QuantAssembly.Dashboard**](QuantAssembly.Dashboard/readme.md): This is a companion application using Blazor WASM to visualize the performance of strategies from both QuantAssembly and QuantAssembly.Backtest

Despite the pretentious name, the current version supports straightforward condition-based strategies, though its architecture is built to accommodate more sophisticated quantitative strategies in the future, depending on evolving interests and needs. The application presently integrates with the Interactive Brokers (IB) Gateway API to retrieve market data and execute trades, but it is designed for easy extension to other data providers and execution platforms.

## Building QuantAssembly
1. Clone the repo: `git clone https://github.com/adit-hasan-dev/QuantAssembly.git`
2. Install [.Net 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
3. Open a powershell window and change directory to the root folder of repo and run the following commands to build the solution. This will build all 3 applications in one go:
   
    ```powershell
    dotnet restore
    dotnet build
    ```
4. Run the unit tests:
    ```powershell
    dotnet test
    ```

### Running the QuantAssembly application
To run the main trading application, you would need to install the IBKR Gateway application that it uses to receive real time data and send orders.
1. To launch an application, change directory to the appropriate project folder and run:
    ```powershell
    dotnet run
    ```
2. **Install IB Gateway**: Follow the steps below to install IB Gateway.
   - **Create an IBKR Account**: If you don't already have an account, you need to create one. Follow the instructions on the [IBKR website](https://www.interactivebrokers.com/en/index.php?f=46345).
   - **Download and Install IB Gateway**: Download the IB Gateway application from the [IBKR Software](https://www.interactivebrokers.com/en/index.php?f=5041) page. Follow the installation instructions provided.
3. **Configure IB Gateway**:
   - **Add Account ID**: Add your IBKR account ID to the `appsettings.json` file in the root of your project. And the relevant keys to the config (configured to be `appsettings.json` by default).
4. **Define Strategies**: Create your trading strategies in JSON format and store them in a directory.
5. **Load strategies and securities**: Use the `TickerStrategyMap` in appsettings.json to define which tickers to monitor and the corresponding strategy to use on them. For example:
   
    ```json
    "TickerStrategyMap": {
        "SPY": "strategies/SPY_strategy.json",
        "AAPL": "strategies/AAPL_strategy.json",
        "GOOGL": "strategies/GOOGL_strategy.json"
        }
    ```
To avoid conflicting signals, a ticker can only be mapped to a single strategy.

1. Please refer to the [Configuration](#configuration) section to set up settings and authentication credentials. The application currently only uses free APIs so it is easy to create an account and get the credentials needed. Devs can upgrade to a paid subscription for the platforms or implement another provider at their discretion. Please refer to [QuantAssembly readme](QuantAssembly/readme.md) for more information on the architecture and design of the main application.
2. **Launch the IB Gateway** application before launching the application.
3. Navigate to the QuantAssembly project folder and run: `dotnet run`.

## How Strategies Work
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

## Configuration

The application uses configurations defined in the `appsettings.json` to store parameters that often require tweaking. Settings that only apply to a specific implementation are stored in the `CustomProperties` field. An example appsettings.json file:
```json
{
    "AccountId": "<account_id>",
    "EnableDebugLog": "true",
    "LogFilePath": "app.log",
    "LedgerFilePath": "ledger.log",
    "PollingIntervalInMs": "30000",
    "TickerStrategyMap": {
        "AMZN": "Strategy/MACD_RSI_Strategy.json",
        "AAPL": "Strategy/MACD_RSI_Strategy.json",
        "MSFT": "Strategy/MACD_RSI_Strategy.json",
        "NVDA": "Strategy/MACD_RSI_Strategy.json"
    },
    "APIKey": "<alpha_vantage_api_key>", // deprecated
    "RiskManagement": {
        "GlobalStopLoss": 5000,
        "MaxDrawDownPercentage": 0.6
    },
    "CustomProperties": {
        "AlpacaMarketsClientConfig": {
            "apiKey": "<api_key>",
            "apiSecret": "<api_secret>"
        },
        "PercentageAccountValueRiskManagerConfig": {
            "MaxTradeRiskPercentage": 0.1
        }
    }
}
```

## Future Improvements
1. Support for defining complex quant strategies using ML, news feeds (maybe we can shove an LLM in here as well? Because why not?)
2. More data types and indicators for strategy evaluation
3. More powerful and robust strategy definition mechanism
4. Support for options and futures
5. Multi-leg trade strategies
6. More sophisticated risk management functionality

## Conclusion
Quant Assembly is an evolving trading bot aimed at delving into the intricacies of quantitative finance. Although it is still under development, it offers a robust framework for building and testing trading strategies. This project not only serves as a valuable learning tool but also provides a solid foundation for those looking to create more advanced trading applications. Hopefully, it will inspire and assist others in their journey through quantitative finance and algorithmic trading. 

Happy trading! 📈🚀
