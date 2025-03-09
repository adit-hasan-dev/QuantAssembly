using System.Buffers.Text;
using QuantAssembly.Common.Models;

namespace QuantAssembly.Common
{
    public class FMPClient : BaseHttpClient
    {
        private readonly string _apiKey;
        private const string baseUrl = "https://financialmodelingprep.com/stable";

        public FMPClient(HttpClient httpClient, string apiKey)
            : base(httpClient)
        {
            _apiKey = apiKey;
        }

        public async Task<FMPKeyMetricsResponse> GetFinancialKeyMetricsAsync(string symbol, int limit = 1, CancellationToken cancellationToken = default)
        {
            var queryParams = new List<string>
            {
                $"symbol={Uri.EscapeDataString(symbol)}",
                $"limit={limit}",
                $"apiKey={Uri.EscapeDataString(_apiKey)}"
            };
            string requestUrl = $"{baseUrl}/key-metrics?{string.Join("&", queryParams)}";
            return await GetAsync<FMPKeyMetricsResponse>(requestUrl);
        }
    }
}