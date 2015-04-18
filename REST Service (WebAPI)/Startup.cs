using System.Web;
using System.Web.Http;
using Finsa.WebApi.HelpPage.AnyHost;
using Microsoft.Owin;
using Ninject;
using Ninject.Web.Common.OwinHost;
using Ninject.Web.WebApi.OwinHost;
using Owin;
using PommaLabs.KVLite;
using PommaLabs.KVLite.Web.Http;
using RestService.Mvc;

[assembly: OwinStartup(typeof(Startup))]

namespace RestService.Mvc
{
    public sealed class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            app.UseNinjectMiddleware(CreateKernel).UseNinjectWebApi(config);
            ConfigureWebApi(app, config);
        }

        private static StandardKernel CreateKernel()
        {
            return new StandardKernel(new NinjectConfig());
        }

        private static void ConfigureWebApi(IAppBuilder app, HttpConfiguration config)
        {
            // REQUIRED TO ENABLE HELP PAGES :)
            config.MapHttpAttributeRoutes(new HelpDirectRouteProvider());
            var xmlDocPath = HttpContext.Current.Server.MapPath(@"~/App_Data/HelpPages/WebServiceHelp.xml");
            config.SetDocumentationProvider(new XmlDocumentationProvider(xmlDocPath));

            // Enables KVLite based output caching.
            ApiOutputCache.RegisterAsCacheOutputProvider(config, CreateKernel().Get<ICache>());

            // Add WebApi to the pipeline.
            app.UseWebApi(config);
        }
    }
}
