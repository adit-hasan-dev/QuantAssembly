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