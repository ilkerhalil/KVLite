using IdentityServer4.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;

namespace PommaLabs.KVLite.Examples.AspNetCore
{
    public sealed class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            // Add IdentityServer4 services.
            services.AddIdentityServer(o =>
            {
                o.Caching.ClientStoreExpiration = TimeSpan.FromMinutes(1);
                o.Caching.CorsExpiration = TimeSpan.FromMinutes(2);
                o.Caching.ResourceStoreExpiration = TimeSpan.FromMinutes(3);
            })
                .AddDeveloperSigningCredential()
                .AddInMemoryApiResources(Identity.GetApiResources())
                .AddInMemoryClients(Identity.GetClients())
                .AddKVLiteCaching()
                .AddResourceStoreCache<InMemoryResourcesStore>()
                .AddClientStoreCache<InMemoryClientStore>();

            // Add custom NodaTime clock.
            services.AddSingleton<IClock>(NetworkClock.Instance);

            // Add KVLite caching services.
            services.AddKVLitePersistentSQLiteCache(s =>
            {
                s.CacheFile = "AspNetCoreCache.sqlite";
                s.DefaultDistributedCacheAbsoluteExpiration = Duration.FromSeconds(30);
            });

            // Add Session service, which will rely on KVLite distributed cache.
            services.AddSession();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.Map("/identity", identityApp =>
            {
                identityApp.UseIdentityServer();
            });

            // IMPORTANT: This session call MUST go before UseMvc().
            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
