using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Tracker.Data;
using Schmellow.DiscordServices.Tracker.Services;
using System;

namespace Schmellow.DiscordServices.Tracker.Controllers
{
    [Route("history")]
    [Authorize(AuthenticationSchemes = "Cookies")]
    public class HistoryController : Controller
    {
        private readonly ILogger<PingController> _logger;
        private readonly IUserStorage _userStorage;
        private readonly HistoryService _historyService;

        public HistoryController(
            ILogger<PingController> logger,
            IUserStorage userStorage,
            HistoryService historyService)
        {
            _logger = logger;
            _userStorage = userStorage;
            _historyService = historyService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Pings", "History");
        }

        [HttpGet("pings")]
        public IActionResult Pings()
        {
            var viewerName = HttpContext?.User?.Identity?.Name;
            var viewer = _userStorage.GetUser(viewerName);
            var vm = _historyService.GetPingsData(viewer);
            return View(vm);
        }

        [HttpGet("pings/{pingId:int}")]
        public IActionResult PingLinks([FromRoute] int pingId)
        {
            var viewerName = HttpContext?.User?.Identity?.Name;
            var viewer = _userStorage.GetUser(viewerName);
            var vm = _historyService.GetPingLinksData(pingId, viewer);
            if (vm == null)
                return NotFound();
            return View(vm);
        }

        [HttpGet("pings/{pingId:int}/actions")]
        public IActionResult PingActions([FromRoute] int pingId)
        {
            var viewerName = HttpContext?.User?.Identity?.Name;
            var viewer = _userStorage.GetUser(viewerName);
            var vm = _historyService.GetPingActionsData(pingId, viewer);
            if (vm == null)
                return NotFound();
            return View(vm);
        }

        [HttpGet("pings/{pingId:int}/{linkId:guid}")]
        public IActionResult LinkActions([FromRoute] Guid linkId)
        {
            var viewerName = HttpContext?.User?.Identity?.Name;
            var viewer = _userStorage.GetUser(viewerName);
            var vm = _historyService.GetLinkActionsData(linkId, viewer);
            if (vm == null)
                return NotFound();
            return View(vm);
        }

        [HttpGet("users")]
        public IActionResult Users()
        {
            var viewerName = HttpContext?.User?.Identity?.Name;
            var viewer = _userStorage.GetUser(viewerName);
            var vm = _historyService.GetUsersData(viewer);
            return View(vm);
        }

        [HttpGet("users/{user}")]
        public IActionResult UserActions([FromRoute] string user)
        {
            var viewerName = HttpContext?.User?.Identity?.Name;
            var viewer = _userStorage.GetUser(viewerName);
            var vm = _historyService.GetUserActionsData(user, viewer);
            return View(vm);
        }
    }
}
