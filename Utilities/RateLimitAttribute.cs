using Microsoft.AspNetCore.Mvc.Filters;
using Location.Interfaces.Services;
using Location.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Location.Utilities
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RateLimitAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int _requestsPerMinute;
        private readonly int _requestsPerHour;

        public RateLimitAttribute(int requestsPerMinute, int requestsPerHour)
        {
            _requestsPerMinute = requestsPerMinute;
            _requestsPerHour = requestsPerHour;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Resolve ICacheService from the IServiceProvider
            var cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();

            string ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            DateTime now = DateTime.UtcNow;

            // Create unique cache keys based on IP address and current time
            string minuteKey = $"RateLimit:{ipAddress}:{now:yyyyMMddHHmm}";
            string hourKey = $"RateLimit:{ipAddress}:{now:yyyyMMddHH}";

            // Check and update minute limit
            RateLimit minuteCache = await cache.GetFromCacheAsync<RateLimit>(
                minuteKey,
                () => Task.FromResult(new RateLimit { Value = 0 }),
                TimeSpan.FromMinutes(1));
            int minuteCount = minuteCache.Value;

            if (minuteCount >= _requestsPerMinute)
            {
                context.Result = new JsonResult(new { message = "Too many requests in a minute" })
                {
                    StatusCode = (int)HttpStatusCode.TooManyRequests // Too Many Requests
                };
                return;
            }

            // Check and update hour limit
            RateLimit hourCache = await cache.GetFromCacheAsync<RateLimit>(
                hourKey,
                () => Task.FromResult(new RateLimit { Value = 0 }),
                TimeSpan.FromHours(1));
            int hourCount = hourCache.Value;

            if (hourCount >= _requestsPerHour)
            {
                context.Result = new JsonResult(new { message = "Too many requests in an hour" })
                {
                    StatusCode = (int)HttpStatusCode.TooManyRequests // Too Many Requests
                };
                return;
            }

            // Increment counts and update cache
            await cache.StoreToCacheAsync(minuteKey, new RateLimit { Value = minuteCount + 1 }, TimeSpan.FromMinutes(1));
            await cache.StoreToCacheAsync(hourKey, new RateLimit { Value = hourCount + 1 }, TimeSpan.FromHours(1));

            await next();
        }
    }
}
