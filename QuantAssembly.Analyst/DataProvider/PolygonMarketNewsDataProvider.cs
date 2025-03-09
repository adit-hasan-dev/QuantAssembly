using QuantAssembly.Common;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;

namespace QuantAssembly.Analyst.DataProvider
{
    public class PolygonMarketNewsDataProvider : IMarketNewsDataProvider
    {
        private readonly PolygonClient client;
        private readonly ILogger logger;

        public PolygonMarketNewsDataProvider(PolygonClient client, ILogger logger)
        {
            this.client = client;
            this.logger = logger;
        }

        public async Task<List<MarketNewsArticle>> GetMarketNewsAsync(string symbol, DateTime? earliestPublishTime, int limit = 10)
        {
            logger?.LogInfo($"[{nameof(PolygonMarketNewsDataProvider)}] Retrieving market news for symbol: {symbol}");
            var response = await client.GetNewsAsync(symbol, limit, earliestPublishTime ?? DateTime.UtcNow.AddMonths(-2));

            List<MarketNewsArticle> marketNewsArticles = response.Results.Select(result =>
            {
                return new MarketNewsArticle{
                    Title = result.Title,
                    Author = result.Author,
                    TickersMentioned = result.Tickers,
                    Description = result.Description,
                    PublishedUTC = result.PublishedUtc,
                    Insight = result.Insights
                        .Where(i => i.Ticker.Equals(symbol, StringComparison.InvariantCultureIgnoreCase))
                        .Select(i => new MarketNewsInsight{ Sentiment = i.Sentiment, Reasoning = i.SentimentReasoning})
                        .First(),
                    Keywords = result.Keywords
                };
            }).ToList();

            logger?.LogInfo($"Successfully retrieved {marketNewsArticles.Count()} articles for symbol: {symbol}");
            return marketNewsArticles;
        }
    }
}