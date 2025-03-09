using QuantAssembly.Common.Models;

namespace QuantAssembly.Analyst.DataProvider
{
    public interface IMarketNewsDataProvider
    {
        Task<List<MarketNewsArticle>> GetMarketNewsAsync(string symbol, DateTime? earliestPublishTime, int limit = 10);
    }
}