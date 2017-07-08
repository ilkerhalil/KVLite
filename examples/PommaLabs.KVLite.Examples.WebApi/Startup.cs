// File name: Startup.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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

using Microsoft.Owin;
using Ninject;
using Ninject.Web.Common.OwinHost;
using Ninject.Web.WebApi.OwinHost;
using Owin;
using PommaLabs.KVLite.Examples.WebApi;
using PommaLabs.KVLite.WebApi;
using Swashbuckle.Application;
using System.Web.Hosting;
using System.Web.Http;

[assembly: OwinStartup(typeof(Startup))]

namespace PommaLabs.KVLite.Examples.WebApi
{
    /// <summary>
    ///   Configures the example service.
    /// </summary>
    public sealed class Startup
    {
        /// <summary>
        ///   Configures the example service.
        /// </summary>
        /// <param name="app">OWIN builder.</param>
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            app.UseNinjectMiddleware(CreateKernel).UseNinjectWebApi(config);
            ConfigureWebApi(app, config);
        }

#pragma warning disable CC0022 // Should dispose object

        private static StandardKernel CreateKernel() => new StandardKernel(new NinjectConfig());

#pragma warning restore CC0022 // Should dispose object

        private static void ConfigureWebApi(IAppBuilder app, HttpConfiguration config)
        {
            // REQUIRED TO ENABLE HELP PAGES :)
            config.MapHttpAttributeRoutes();
            config.EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", "KVLite.WebApiCaching");
                c.IncludeXmlComments(HostingEnvironment.MapPath(@"~/App_Data/HelpPages/WebServiceHelp.xml"));
            }).EnableSwaggerUi(c =>
            {
                c.DocExpansion(DocExpansion.None);
            });

            // Enables KVLite based output caching.
            OutputCacheProvider.Register(config, CreateKernel().Get<ICache>());

            // Add WebApi to the pipeline.
            app.UseWebApi(config);
        }
    }
}
