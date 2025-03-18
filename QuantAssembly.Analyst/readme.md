# QuantAssembly.Analyst

This application is built using Azure Semantic Kernel and Azure OpenAI Service and uses LLMs and quantitative analysis to provide trading recommendations. It combines multiple procedural rules with LLM calls to ultimately generating a report with recommended options contracts.

## Architecture
Here are the main components:

- **Analyst**: This is the orchestrator that brings all subsystems together and carries out the main logic of the application. It initializes dependencies, builds the processing pipeline, and executes it.

- **IMarketDataProvider**: Interface for retrieving real-time market data like the latest price quotes, volume, etc. The implementation used is based on the Alpaca Markets API.

- **IIndicatorDataProvider**: Interface for retrieving historical market data and indicators like historical prices, RSI, EMA, etc. The implementation used is based on the Alpaca Markets API.

- **IOptionsChainDataProvider**: Interface for retrieving options chain data. The implementation used is based on the Alpaca Markets API.

- **ILLMService**: Interface for interacting with language models to generate insights and recommendations. The implementation used is based on Azure OpenAI Service.

- **Pipeline**: The processing pipeline consists of several steps that filter and analyze market data:
  - **InitStep**: Initializes the context and loads necessary data.
  - **StockDataFilterStep**: Filters stocks based on predefined criteria.
  - **IndicatorFilterStep**: Filters stocks based on technical indicators.
  - **OptionsFilterStep**: Filters options contracts based on predefined criteria.
  - **PreAIStep**: Prepares data for analysis by the language model.
  - **LLMStep**: Interacts with the language model to generate insights and recommendations.
    - **Curator System**: Analyzes a list of ticker symbols and their respective companies to identify strong signals for directional trades. It uses both quantitative and qualitative analysis and can use function calling using Semantic Kernel plugins to retrieve recent market news to refine its analysis.
    - **TradeManager System**: Reviews a list of ticker symbols and their options contracts to recommend up to 5 contracts to buy. It aims to maximize profitability and minimize risk by analyzing each contract's profitability, risk profile, and anticipated changes in value.
  - **PresentationStep**: Formats the output and generates the final report to be saved to disk.

## Running the QuantAssembly.Analyst application
To run the analyst application, follow these steps:

1. **Clone the repo**: `git clone https://github.com/adit-hasan-dev/QuantAssembly.git`
2. **Install [.Net 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)**
3. **Build the solution**:
    ```powershell
    dotnet restore
    dotnet build
    ```
4. **Configure the application**:
   - Update the `appsettings.json` file with the necessary API keys and configuration settings.
   - Example `appsettings.json`:
     ```json
     {
         "EnableDebugLog": "false",
         "LogFilePath": "app.log",
         "MarketDataFilterConfig": {
             "MinPrice": 5.0,
             "MaxPrice": 1500.0,
             "MinVolume": 100
         },
         "CompanyDataFilterConfig": {
             "MinimumCompanyAge": 10,
             "MaximumCompanyAge": null,
             "MinimumMarketCap": null,
             "Sectors": [],
             "SubIndustries": []
         },
         "IndicatorFilterConfig": {
             "RSIOversoldThreshold": 30.0,
             "RSIOverboughtThreshold": 70.0
         },
         "OptionsContractFilterConfig": {
             "MinimumOpenInterest": 500,
             "MaxBidAskSpread": 0.05
         },
         "OutputFilePath": "C:/Users/adith/Documents/OptionsReports",
         "CustomProperties": {
             "AlpacaMarketsClientConfig": {
                 "apiKey": "<api_key>",
                 "apiSecret": "<api_secret>",
                 "batchSize": 20
             },
             "AzureOpenAIServiceClientConfig": {
                 "apiKey": "<api_key>",
                 "endpoint": "<endpoint>",
                 "deploymentName": "gpt-4o"
             },
             "PolygonClientConfig": {
                 "apiKey": "<api_key>",
                 "maxCallsPerMin": 5
             }
         }
     }
     ```
5. **Run the application**:
    ```powershell
    dotnet run
    ```

## Future Improvements
- Enhance the filtering criteria for more accurate recommendations.
- Integrate premium API subscriptions to remove rate limits and speed up execution.