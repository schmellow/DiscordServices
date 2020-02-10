using Microsoft.AspNetCore.Mvc;

namespace Schmellow.DiscordServices.Tracker.Controllers
{
    [Route("error")]
    public class ErrorController : Controller
    {
        [Route("")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
