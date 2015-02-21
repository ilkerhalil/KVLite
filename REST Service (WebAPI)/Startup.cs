using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Owin;
using Owin;
using RestService.Mvc;

[assembly: OwinStartup(typeof(Startup))]

namespace RestService.Mvc
{
    public sealed class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Inizializzatori di default per MVC & Web API.
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}