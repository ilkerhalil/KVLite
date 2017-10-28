using Microsoft.AspNetCore.Mvc;
using NodaTime;
using PommaLabs.KVLite.AspNetCore.Http;
using System;

namespace PommaLabs.KVLite.Examples.AspNetCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IClock _clock;

        public HomeController(IClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public IActionResult Index()
        {
            HttpContext.Session.SetObject("lastVisit", _clock.GetCurrentInstant());

            return View();
        }

        public IActionResult About()
        {
            var lastVisit = HttpContext.Session.GetObject<Instant>("lastVisit").ValueOrDefault();
            ViewData["Message"] = $"Your application description page. Last visit at {lastVisit}.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
