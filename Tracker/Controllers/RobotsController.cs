using Microsoft.AspNetCore.Mvc;

namespace Schmellow.DiscordServices.Tracker.Controllers
{
    public class RobotsController : Controller
    {
        [Route("robots.txt")]
        public IActionResult Index()
        {
            Response.ContentType = "text/plain";
            return View();
        }
    }
}
