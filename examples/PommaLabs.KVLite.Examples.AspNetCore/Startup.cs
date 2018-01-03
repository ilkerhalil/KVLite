// File name: Startup.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2018 Alessio Parma <alessio.parma@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using IdentityServer4.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PommaLabs.KVLite.Extensibility;
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
            services.AddSingleton<IClock>(SystemClock.Instance);

            // Add KVLite caching services.
            services.AddKVLitePersistentSQLiteCache(s =>
            {
                s.CacheFile = "AspNetCoreCache.sqlite";
                s.DefaultDistributedCacheAbsoluteExpiration = TimeSpan.FromSeconds(30);
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
