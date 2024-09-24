using System.Collections.Concurrent;
using System.Text.Json;
using Location.Interfaces.Services;
using Location.Models;
using Location.Utilities;

namespace Location.Services.LocationLogic
{
    public class ProviderSelectorLogic : IProviderSelectorLogic
    {
        private readonly List<Provider> _providers;
        private readonly ConcurrentDictionary<string, ProviderQualityMetrics> _providerMetrics;
        private readonly ICacheService _cache;
        private const int RequestThreshold = 45;
        public ProviderSelectorLogic(ICacheService cache)
        {
            _cache = cache;

            // Load providers from configuration
            _providers = ConfigData.GetIpLocationProviders();

            // Initialize metrics for each provider from cache or create new if not found
            _providerMetrics = new ConcurrentDictionary<string, ProviderQualityMetrics>(
                _providers.Select(p => new KeyValuePair<string, ProviderQualityMetrics>(p.Name, LoadMetricsFromCacheAsync(p.Name).Result))
            );
        }

        public async Task<Provider> GetBestProviderAsync(string? excludeProviderName = null)
        {
            var now = DateTime.Now;
            // Check if any provider has reached the request threshold if yes reset all this helps balancing the load equally
            var anyProviderReachedThreshold = _providerMetrics.Values.Any(p => p.RequestCount >= RequestThreshold);

            // Update metrics for each provider
            foreach (var provider in _providerMetrics.Values)
            {
                // Reset metrics if more than 5 minutes have passed
                // we are storing the error and response tome metrics for 5 minutes for best performances
                if ((now - provider.LastResetTime).TotalMinutes >= 5)
                {
                    await ResetMetricsAsync(provider.ProviderName);
                }
            }
            
            if (anyProviderReachedThreshold)
            {
                foreach (var provider in _providerMetrics.Values)
                {
                    await ResetMetricsAsync(provider.ProviderName);
                }
            }

            IEnumerable<ProviderQualityMetrics> query = _providerMetrics.Values;

            // Perform the exclusion first if excludeProviderName is not null or whitespace
            if (!string.IsNullOrWhiteSpace(excludeProviderName))
            {
                query = query.Where(p => p.ProviderName != excludeProviderName);
            }

            // the provider which got least request get the chance first
            // as well as we also check the provider with least error
            // then the best response time
            // algo mix of round-robin & a little tweaked weighted connections so here we assume error and repsonse time as their weight capabilities to handle a request
            var bestProviderMetrics = query
                .OrderBy(p => p.RequestCount)
                .ThenBy(p => p.ErrorCount)
                .ThenBy(p => p.AvgResponseTime)
                .FirstOrDefault();


            Console.WriteLine(_providers.FirstOrDefault(p => p.Name == bestProviderMetrics?.ProviderName).Name);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true // For pretty-printing the JSON
            };
            string metricsJson = JsonSerializer.Serialize(bestProviderMetrics, options);
            Console.WriteLine(metricsJson);
            return _providers.FirstOrDefault(p => p.Name == bestProviderMetrics?.ProviderName) ?? new Provider();
        }

        private async Task<ProviderQualityMetrics> LoadMetricsFromCacheAsync(string providerName)
        {
            // Fetch metrics from cache with a timeout and default callback
            var metrics = await _cache.GetFromCacheAsync<ProviderQualityMetrics>(providerName, async () => await GetDefaultProviderMetrics(providerName), TimeSpan.FromMinutes(5));

            // Set default values if not found in cache
            if (metrics == null)
            {
                metrics = new ProviderQualityMetrics { ProviderName = providerName };
                await _cache.StoreToCacheAsync(providerName, metrics, TimeSpan.FromMinutes(5));
            }
            return metrics;
        }

        public async Task UpdateMetricsAsync(string providerName, bool isError, double responseTime)
        {
            if (_providerMetrics.TryGetValue(providerName, out var metrics))
            {
                if (isError) metrics.ErrorCount++;
                else
                {
                    metrics.ResponseTimes.Add(responseTime);
                    metrics.AvgResponseTime = metrics.ResponseTimes.Average();
                }

                metrics.RequestCount++;
                //  chosen 45 as the default request threshold after a provider reach 45(RequestThreshold) mark we reset it 0 
                // this has been done to give every provider equal chance
                // on the other hand we also have given priority to error count and avg response time 
                // if a provider is getting a lot of error and response time is also less then it is most likely to get very less requests if it has lots of requests left
                if (metrics.RequestCount >= RequestThreshold)
                {
                    await ResetMetricsAsync(providerName);
                }
                metrics.ProviderScore += 1; //  everytime a provider returns a result we score it it helps choosing the best provider
                                            // Update metrics in cache
                await _cache.StoreToCacheAsync(providerName, metrics, TimeSpan.FromMinutes(5));
            }
        }

        private async Task ResetMetricsAsync(string providerName)
        {
            if (_providerMetrics.TryGetValue(providerName, out var metrics))
            {
                metrics.RequestCount = 0;
                metrics.ErrorCount = 0;
                metrics.ResponseTimes.Clear();
                metrics.AvgResponseTime = 0;
                metrics.LastResetTime = DateTime.Now;

                // Update metrics in cache
                await _cache.StoreToCacheAsync(providerName, metrics, TimeSpan.FromMinutes(5));
            }
        }

        private async Task<ProviderQualityMetrics> GetDefaultProviderMetrics(string providerName)
        {
            return new ProviderQualityMetrics
            {
                ProviderName = providerName
            };
        }
    }
}
