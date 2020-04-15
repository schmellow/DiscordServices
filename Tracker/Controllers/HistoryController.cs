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
        public IActionResult Index(int page = 1)
        {
            var userName = HttpContext?.User?.Identity?.Name;
            var user = _userStorage.GetUser(userName);
            var vm = _historyService.GetIndexData(page, user);
            return View(vm);
        }

        [HttpGet("{pingId:int}")]
        public IActionResult Ping(int pingId)
        {
            var userName = HttpContext?.User?.Identity?.Name;
            var user = _userStorage.GetUser(userName);
            var vm = _historyService.GetPingData(pingId, user);
            if (vm == null)
                return NotFound();
            return View(vm);
        }

        [HttpGet("{pingId:int}/timeline")]
        public IActionResult Timeline(int pingId)
        {
            var userName = HttpContext?.User?.Identity?.Name;
            var user = _userStorage.GetUser(userName);
            var vm = _historyService.GetTimelineData(pingId, user);
            if (vm == null)
                return NotFound();
            return View(vm);
        }

        [HttpGet("{pingId:int}/{linkId:guid}")]
        public IActionResult Link(Guid linkId)
        {
            var userName = HttpContext?.User?.Identity?.Name;
            var user = _userStorage.GetUser(userName);
            var vm = _historyService.GetLinkData(linkId, user);
            if (vm == null)
                return NotFound();
            return View(vm);
        }
    }
}
