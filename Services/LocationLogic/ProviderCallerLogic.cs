using System.Diagnostics;
using Location.Interfaces.Services;
using Location.Models;

namespace Location.Services.LocationLogic
{
    public class ProviderCallerLogic : IProviderCallerLogic
    {
        private readonly IProviderSelectorLogic _providerSelector;

        public ProviderCallerLogic(IProviderSelectorLogic providerSelector)
        {
            _providerSelector = providerSelector;
        }

        /// <summary>
        /// Calls the API of the selected provider, updates metrics, and returns the JSON response.
        /// Includes fallback logic in case the primary provider fails.
        /// </summary>
        /// <param name="provider">The provider containing API details.</param>
        /// <param name="ipAddress">The IP address to query.</param>
        /// <returns>The JSON response from the provider API.</returns>
        public async Task<string> CallProviderApiAsync(Provider provider, string ipAddress)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new ArgumentException("IP address cannot be null or empty.", nameof(ipAddress));
            }

            try
            {
                // Attempt to call the primary provider
                return await AttemptProviderCallAsync(provider, ipAddress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Primary provider failed: {ex.Message}");
                // If the primary provider fails, try the fallback logic to handle the request using another provider
                return await HandleFallbackAsync(ipAddress, provider.Name);
            }
        }

        /// <summary>
        /// Attempts to call the API of the specified provider.
        /// Measures response time, updates metrics, and returns the JSON response.
        /// </summary>
        private async Task<string> AttemptProviderCallAsync(Provider provider, string ipAddress)
        {
            string responseContent = string.Empty;
            string requestUrl = string.Format(provider.Url, ipAddress, provider.Token);

            // Measure the response time
            var stopwatch = Stopwatch.StartNew();
            using (var httpClient = new HttpClient())
            {
                // Send the request and get the response
                var response = await httpClient.GetAsync(requestUrl);
                stopwatch.Stop();
                double responseTime = stopwatch.Elapsed.TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    responseContent = await response.Content.ReadAsStringAsync();
                    // Update metrics as a successful request
                    await UpdateMetricsAsync(provider.Name, false, responseTime);
                }
                else
                {
                    // Update metrics as an error
                    await UpdateMetricsAsync(provider.Name, true, responseTime);
                    return await HandleFallbackAsync(ipAddress, provider.Name);
                }
            }
            return responseContent;
        }

        /// <summary>
        /// Handles fallback logic in case the selected provider fails.
        /// Calls the next best provider based on updated metrics.
        /// </summary>
        private async Task<string> HandleFallbackAsync(string ipAddress, string failedProviderName)
        {
            var nextBestProvider = await _providerSelector.GetBestProviderAsync(excludeProviderName: failedProviderName);
            if (nextBestProvider == null)
            {
                throw new ArgumentNullException("No fallback providers available.", nameof(nextBestProvider));
            }
            try
            {
                return await AttemptProviderCallAsync(nextBestProvider, ipAddress);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task UpdateMetricsAsync(string providerName, bool isError, double responseTime)
        {
            await _providerSelector.UpdateMetricsAsync(providerName, isError, responseTime);
        }
    }
}
