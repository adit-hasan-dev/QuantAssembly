using System.Text.Json.Serialization;
using QuantAssembly.Common.Models;

namespace QuantAssembly.Common
{
    public class PolygonClient : BaseHttpClient
    {
        private readonly string _apiKey;
        private const string BaseNewsUrl = "https://api.polygon.io/v2/reference/news";

        public PolygonClient(HttpClient httpClient, string apiKey)
            : base(httpClient)
        {
            _apiKey = apiKey;
        }

        public async Task<PolygonNewsResponse> GetNewsAsync(
            string ticker, 
            int limit,
            DateTime earliestPublishTime, 
            string cursor = null, 
            CancellationToken cancellationToken = default)
        {
            // Build query parameters.
            var queryParams = new List<string>
            {
                $"ticker={Uri.EscapeDataString(ticker)}",
                $"limit={limit}",
                $"$published_utc.gt={earliestPublishTime:yyyy-MM-ddTHH:mm:sszzz}",
                $"apiKey={Uri.EscapeDataString(_apiKey)}"  // API key appended as query parameter.
            };

            if (!string.IsNullOrWhiteSpace(cursor))
            {
                queryParams.Add($"cursor={Uri.EscapeDataString(cursor)}");
            }

            string requestUrl = $"{BaseNewsUrl}?{string.Join("&", queryParams)}";
            return await GetAsync<PolygonNewsResponse>(requestUrl, cancellationToken);
        }
    }
}