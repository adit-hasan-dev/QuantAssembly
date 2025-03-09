using System.Text.Json.Serialization;

namespace QuantAssembly.Common.Models
{
    public class Insight
    {
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; }

        [JsonPropertyName("sentiment")]
        public string Sentiment { get; set; }

        [JsonPropertyName("sentiment_reasoning")]
        public string SentimentReasoning { get; set; }
    }

    public class Publisher
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("homepage_url")]
        public string HomepageUrl { get; set; }

        [JsonPropertyName("logo_url")]
        public string LogoUrl { get; set; }

        [JsonPropertyName("favicon_url")]
        public string FaviconUrl { get; set; }
    }

    public class Result
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("publisher")]
        public Publisher Publisher { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("published_utc")]
        public DateTime PublishedUtc { get; set; }

        [JsonPropertyName("article_url")]
        public string ArticleUrl { get; set; }

        [JsonPropertyName("tickers")]
        public List<string> Tickers { get; set; } = new List<string>();

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new List<string>();

        [JsonPropertyName("insights")]
        public List<Insight> Insights { get; set; } = new List<Insight>();
    }

    public class PolygonNewsResponse
    {
        [JsonPropertyName("results")]
        public List<Result> Results { get; set; } = new List<Result>();

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("request_id")]
        public string RequestId { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next_url")]
        public string NextUrl { get; set; }
    }
}