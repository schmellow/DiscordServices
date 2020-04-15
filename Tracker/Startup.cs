using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Schmellow.DiscordServices.Tracker.Data;
using Schmellow.DiscordServices.Tracker.Models;
using Schmellow.DiscordServices.Tracker.Services;

namespace Schmellow.DiscordServices.Tracker
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication()
                .AddScheme<TokenAuthenticationOptions, TokenAuthenticationHandler>(TokenAuthenticationHandlerDefaults.AuthenticationScheme, null)
                .AddCookie(options =>
                {
                    options.LoginPath = "/auth/login";
                    options.LogoutPath = "/auth/logout";
                    options.ReturnUrlParameter = "";
                    options.Events.OnValidatePrincipal += CookieValidator.ValidateAsync;
                });
            services.AddSingleton<LiteDBStorage>(p => new LiteDBStorage(p.GetRequiredService<TrackerProperties>()));
            services.AddSingleton<IPingStorage>(p => p.GetRequiredService<LiteDBStorage>());
            services.AddSingleton<IUserStorage>(p => p.GetRequiredService<LiteDBStorage>());
            services.AddSingleton<PingService>();
            services.AddSingleton<HistoryService>();
            services.AddSingleton<ESIClient>();
            services.AddControllersWithViews().AddNewtonsoftJson();
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var props = app.ApplicationServices.GetRequiredService<TrackerProperties>();
            if (!string.IsNullOrEmpty(props.ProxyBasePath))
                app.UsePathBase(props.ProxyBasePath);
            app.UseForwardedHeaders();
            app.UseMiddleware<RequestLogging>();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                if(!props.DisableHttps)
                    app.UseHsts();
            }
            app.UseStatusCodePagesWithReExecute("/error");
            if(!props.DisableHttps)
                app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
