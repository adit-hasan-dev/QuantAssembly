using System.Text.Json;
using Polly;
using Polly.Extensions.Http;
using Polly.RateLimit;

namespace QuantAssembly.Common
{
    /// <summary>
    /// Provides common HTTP call functionality with Polly retries.
    /// </summary>
    public abstract class BaseHttpClient
    {
        protected readonly HttpClient HttpClient;
        protected readonly IAsyncPolicy<HttpResponseMessage> RetryPolicy;
        private readonly AsyncRateLimitPolicy RateLimitPolicy;

        protected BaseHttpClient(HttpClient httpClient, int maxRequestsPerMinute)
        {
            HttpClient = httpClient;

            // Default retry policy to handle transient HTTP errors.
            RetryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );

            // Rate limit policy
            RateLimitPolicy = Policy.RateLimitAsync(maxRequestsPerMinute, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Executes an HTTP GET request against the given URL, applies the retry and rate-limit policies,
        /// checks the response status code, and deserializes the JSON response into T.
        /// </summary>
        protected async Task<T> GetAsync<T>(string requestUrl, CancellationToken cancellationToken = default)
        {
            return await RateLimitPolicy!.ExecuteAsync(async () =>
            {
                HttpResponseMessage response = await RetryPolicy.ExecuteAsync(
                    () => HttpClient.GetAsync(requestUrl, cancellationToken)
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Request to {requestUrl} failed with status code: {response.StatusCode}");
                }

                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result == null)
                {
                    throw new Exception($"Deserialization of response content from {requestUrl} returned null.");
                }
                return result;
            });
        }
    }
}