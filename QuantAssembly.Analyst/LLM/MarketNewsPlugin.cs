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

        [KernelFunction("get_news")]
        [Description("Gets market news articles from the last 2 months for the ticker symbol up to a maximum of 5 articles")]
        public async Task<List<MarketNewsArticle>> GetMarketNews([Description("The ticker symbol to retrieve market news for")]string symbol)
        {
            return await this.marketNewsDataProvider.GetMarketNewsAsync(symbol, DateTime.UtcNow.AddMonths(-1), 5);
        }
    }
}