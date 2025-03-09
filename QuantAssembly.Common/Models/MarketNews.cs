using System.Text.Json.Serialization;

namespace QuantAssembly.Common.Models
{
    public class MarketNewsInsight
    {
        [JsonPropertyName("sentiment")]
        public string Sentiment { get; set; }
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; }
    }
    public class MarketNewsArticle
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("author")]
        public string Author { get; set; }
        [JsonPropertyName("tickers_mentioned")]
        public List<string> TickersMentioned { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("published_utc")]
        public DateTime PublishedUTC { get; set; }
        [JsonPropertyName("insight")]
        public MarketNewsInsight Insight { get; set; }
        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; }
    }
}