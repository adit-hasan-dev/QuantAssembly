# Quant Assembly

This is the main trading application. 

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

- **IIndicatorDataProvider**: Interface for retrieving historical market data and indicators like historical prices, RSI, EMA etc. There are currently two implementations:
  - **StockIndicatorsDataProvider**: This implementation is based the [Stock Indicators for .NET](https://github.com/DaveSkender/Stock.Indicators) library to compute the metrics and [Alpaca Markets API](https://docs.alpaca.markets/) to retrieve historical data. This is the recommended implementation to use since it has the fewest limitations as a free option and is quite easy to use.
  - **AlphaVantageIndicatorDataProvider**: This implementation is based on the [Alpha Vantage API](https://www.alphavantage.co/documentation/) and only uses the free endpoints. The free version of AlphaVantage is very limited (e.g. only 25 calls/day) so unless you are willing to pay for the premium API, this remains the inferior choice.

- **IAccountDataProvider**: Interface for retrieving account and portfolio information. Currently there is one implementation based on IB Gateway, since that is the main platform supported for the application, with trades being executed through the same platform.

- **IStrategyProcessor**: Interface responsible for loading and evaluating trading strategies. It uses the data from the data providers and the defined strategies to produce entry and exit signals. 
 
- **IRiskManager**: Interface responsible for managing trade risks and position sizing. It takes into account available funds in the portfolio, configured global stop loss, max draw down percentage etc and determines if an entry signal produced by a strategy can actually be used to open a position. Currently only supports long positions for stocks. There is a very simple implementation of this that uses account value percentage to size positions. 
  
- **ITradeManager**: Interface responsible for carrying out trades based on the parameters set by the risk manager. The only implementation is based on the IB Gateway API.
- **ILedger**: All open and closed positions are recorded in the Ledger. At launch the ledger is loaded onto memory and every transaction is added to it to be saved to disk. The built-in implementation is a json file with all positions recorded.
- Optionally, you can implement `IMarketDataProvider`, `IIndicatorDataProvider`, `IAccountDataProvider`, `IRiskManager`  and/or `ITradeManager` to use other platforms, or a mixture of several.
