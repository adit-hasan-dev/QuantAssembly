using System.ComponentModel;
using Microsoft.SemanticKernel;
using QuantAssembly.Analyst.DataProvider;
using QuantAssembly.Common.Models;

namespace QuantAssembly.Analyst.LLM
{
    public class MarketNewsPlugin
    {
        private readonly IMarketNewsDataProvider marketNewsDataProvider;

        public MarketNewsPlugin(IMarketNewsDataProvider marketNewsDataProvider)
        {
            this.marketNewsDataProvider = marketNewsDataProvider;
        }

        private const string GetNewsDescription = @"Gets market news articles from the last 2 months for the ticker symbol up to a maximum of 5 articles.

    Returns:
    {
       ""type"": ""array"",
       ""items"": {
         ""type"": ""object"",
         ""properties"": {
           ""title"": { ""type"": ""string"", ""description"": ""Title of the news article"" },
           ""author"": { ""type"": ""string"", ""description"": ""Author of the article"" },
           ""tickers_mentioned"": {
             ""type"": ""array"",
             ""items"": { ""type"": ""string"" },
             ""description"": ""List of ticker symbols mentioned in the article""
           },
           ""description"": { ""type"": ""string"", ""description"": ""Summary of the article's content"" },
           ""published_utc"": { ""type"": ""string"", ""format"": ""date-time"", ""description"": ""UTC publication date and time of the article"" },
           ""insight"": {
             ""type"": ""object"",
             ""properties"": {
               ""sentiment"": { ""type"": ""string"", ""description"": ""Overall sentiment of the article (e.g., positive, negative, neutral)"" },
               ""reasoning"": { ""type"": ""string"", ""description"": ""Explanation supporting the sentiment assessment"" }
             },
             ""description"": ""AI-generated insights regarding the sentiment and its reasoning""
           },
           ""keywords"": {
             ""type"": ""array"",
             ""items"": { ""type"": ""string"" },
             ""description"": ""Key topics or terms related to the article""
           }
         },
         ""required"": [""title"", ""author"", ""tickers_mentioned"", ""description"", ""published_utc"", ""insight"", ""keywords""]
       }
    }

    Example Responses:

    [
      {
        ""title"": ""25% of Warren Buffett-Led Berkshire Hathaway's $288 Billion Portfolio Is Invested in Only 1 Stock"",
        ""author"": ""The Motley Fool"",
        ""tickers_mentioned"": [""AAPL"", ""BRK.A"", ""BRK.B""],
        ""description"": ""Warren Buffett's Berkshire Hathaway has invested 25% of its $288 billion portfolio in Apple, but investors should be cautious about Apple's current growth prospects and valuation."",
        ""published_utc"": ""2025-03-08T14:30:00Z"",
        ""insight"": {
          ""sentiment"": ""negative"",
          ""reasoning"": ""The article suggests that Apple's growth prospects are stagnating, and its valuation is expensive with a P/E ratio of 37.8, a 65% premium to the trailing-10-year average.""
        },
        ""keywords"": [""Warren Buffett"", ""Berkshire Hathaway"", ""Apple""]
      },
      {
        ""title"": ""Apple Intelligence Is Fueling iPhone Upgrades in Positive News for Apple Stock Investors"",
        ""author"": ""The Motley Fool"",
        ""tickers_mentioned"": [""AAPL""],
        ""description"": ""Apple's latest catalyst could increase consumer upgrade activity, which is positive news for Apple stock investors."",
        ""published_utc"": ""2025-03-07T12:02:00Z"",
        ""insight"": {
          ""sentiment"": ""positive"",
          ""reasoning"": ""The article suggests that Apple's latest iPhone features could increase consumer upgrade activity, which benefits stock investors.""
        },
        ""keywords"": [""Apple"", ""iPhone"", ""stock"", ""investors""]
      }
    ]";

        [KernelFunction("get_news")]
        [Description(GetNewsDescription)]
        public async Task<List<MarketNewsArticle>> GetMarketNews([Description("The ticker symbol to retrieve market news for")] string symbol)
        {
            return await this.marketNewsDataProvider.GetMarketNewsAsync(symbol, DateTime.UtcNow.AddMonths(-2), 5);
        }
    }
}