using System.Text.Json;
using Polly;
using Polly.Extensions.Http;

namespace QuantAssembly.Common
{
    /// <summary>
    /// Provides common HTTP call functionality with Polly retries.
    /// </summary>
    public abstract class BaseHttpClient
    {
        protected readonly HttpClient HttpClient;
        protected readonly IAsyncPolicy<HttpResponseMessage> RetryPolicy;

        protected BaseHttpClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
            // Create a default retry policy to handle transient HTTP errors.
            RetryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );
        }

        /// <summary>
        /// Executes an HTTP GET request against the given URL, applies the retry policy,
        /// checks the response status code, and deserializes the JSON response into T.
        /// </summary>
        protected async Task<T> GetAsync<T>(string requestUrl, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await RetryPolicy.ExecuteAsync(
                () => HttpClient.GetAsync(requestUrl, cancellationToken)
            );

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Request to {requestUrl} failed with status code: {response.StatusCode}");

            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    } 
}