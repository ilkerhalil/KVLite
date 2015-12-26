using Finsa.CodeServices.Common.Portability;
using Microsoft.Owin;
using Ninject;
using Ninject.Web.Common.OwinHost;
using Ninject.Web.WebApi.OwinHost;
using Owin;
using PommaLabs.KVLite;
using PommaLabs.KVLite.WebApi;
using RestService.WebApi;
using Swashbuckle.Application;
using System.Web.Http;

[assembly: OwinStartup(typeof(Startup))]
namespace RestService.WebApi
{
    public sealed class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            app.UseNinjectMiddleware(CreateKernel).UseNinjectWebApi(config);
            ConfigureWebApi(app, config);
        }

        static StandardKernel CreateKernel() => new StandardKernel(new NinjectConfig());

        static void ConfigureWebApi(IAppBuilder app, HttpConfiguration config)
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
            ApiOutputCache.RegisterAsCacheOutputProvider(config, CreateKernel().Get<ICache>());

            // Add WebApi to the pipeline.
            app.UseWebApi(config);
        }
    }
}
