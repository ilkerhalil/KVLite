using System.Web;
using System.Web.Http;
using Finsa.WebApi.HelpPage.AnyHost;
using Ninject;
using PommaLabs.KVLite;
using PommaLabs.KVLite.Web.Http;

namespace RestService.Mvc
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // REQUIRED TO ENABLE HELP PAGES :)
            config.MapHttpAttributeRoutes(new HelpDirectRouteProvider());
            var xmlDocPath = HttpContext.Current.Server.MapPath(@"~/App_Data/HelpPage/RestService.Mvc.xml");
            config.SetDocumentationProvider(new XmlDocumentationProvider(xmlDocPath));

            // Enables KVLite based output caching.
            ApiOutputCache.RegisterAsCacheOutputProvider(config, Startup.CreateKernel().Get<ICache>());
        }
    }
}