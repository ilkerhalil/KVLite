using System.Web.Mvc;
using Finsa.WebApi.HelpPage.AnyHost;

namespace RestService.Mvc.Controllers
{
    [RoutePrefix("")]
    public sealed class HomeController : Controller
    {
        [Route("")]
        public ActionResult Index()
        {
            return Redirect(Url.HttpRouteUrl(HelpControllerBase.IndexRouteName, new {}));
        }
    }
}