using Location.Models;

namespace Location.Utilities
{
    public static class ConfigData
    {
        private static IConfiguration _configuration;
        public static void SetConfigValues(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public static string GetConfigValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Configuration key cannot be null or empty", nameof(key));
            }
            return _configuration.GetValue<string>($"AppSettings:{key}");
        }

        public static List<Provider> GetIpLocationProviders()
        {
            var providersSection = _configuration.GetSection("AppSettings:Providers:IpLocationProviders");
            var providers = providersSection.Get<List<Provider>>();
            return providers ?? new List<Provider>();
        }
    }
}
