namespace QuantAssembly.Common.Models
{
    public class MarketNewsInsight
    {
        public string Sentiment { get; set; }
        public string Reasoning { get; set; }
    }
    public class MarketNewsArticle
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public List<string> TickersMentioned { get; set; }
        public string Description { get; set; }
        public DateTime PublishedUTC { get; set; }
        public MarketNewsInsight Insight { get; set; }
        public List<string> Keywords { get; set; }
    }
}