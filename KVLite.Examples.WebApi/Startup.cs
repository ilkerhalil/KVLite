using Microsoft.Owin;
using Ninject;
using Ninject.Web.Common.OwinHost;
using Ninject.Web.WebApi.OwinHost;
using Owin;
using PommaLabs.KVLite;
using PommaLabs.CodeServices.Common.Portability;
using PommaLabs.KVLite.WebApi;
using RestService.WebApi;
using Swashbuckle.Application;
using System.Web.Http;

[assembly: OwinStartup(typeof(Startup))]

namespace RestService.WebApi
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
                c.IncludeXmlComments(PortableEnvironment.MapPath(@"~/App_Data/HelpPages/WebServiceHelp.xml"));
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