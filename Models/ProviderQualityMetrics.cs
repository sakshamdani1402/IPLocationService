using System;
using System.Collections.Generic;
using System.Linq;

namespace Location.Models
{
    /// <summary>
    /// Represents the quality metrics for an IP location provider.
    /// </summary>
    public class ProviderQualityMetrics
    {
        public string ProviderName { get; set; }
        public int RequestCount { get; set; }
        public int ErrorCount { get; set; }
        public double AvgResponseTime { get; set; }
        public DateTime LastResetTime { get; set; } = DateTime.Now;
        public List<double> ResponseTimes { get; set; } = new List<double>();
        public int ProviderScore { get; set; }
    }
}
