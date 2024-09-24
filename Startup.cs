using Location.Interfaces.Services;
using Location.Services.Cache;
using Location.Utilities;
using Location.Services.LocationLogic;

namespace LocationService
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Add memory cache and other services
            services.AddMemoryCache();
            
            // Add logging
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            });

            services.AddSingleton<ICacheService, CacheService>();
            services.AddSingleton<IProviderSelectorLogic, ProviderSelectorLogic>();
            services.AddSingleton<IProviderCallerLogic, ProviderCallerLogic>();
            ConfigData.SetConfigValues(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            // Example of logging an information message
            logger.LogInformation("Application started.");
        }
    }
}
