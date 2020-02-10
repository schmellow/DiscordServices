using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Tracker.Services;
using System;

namespace Schmellow.DiscordServices.Tracker.Controllers
{
    [Route("history")]
    [Authorize(AuthenticationSchemes = "Cookies")]
    public class HistoryController : Controller
    {
        private readonly ILogger<PingController> _logger;
        private readonly HistoryService _historyService;

        public HistoryController(ILogger<PingController> logger, HistoryService historyService)
        {
            _logger = logger;
            _historyService = historyService;
        }

        [HttpGet]
        public IActionResult Index(int page = 1)
        {
            var vm = _historyService.GetIndexData(page);
            return View(vm);
        }

        [HttpGet("{pingId:int}")]
        public IActionResult Ping(int pingId, [FromQuery] int parentPage = 1)
        {
            var vm = _historyService.GetPingData(pingId, parentPage);
            if (vm == null)
                return NotFound();
            return View(vm);
        }

        [HttpGet("{pingId:int}/{linkId:guid}")]
        public IActionResult Link(int pingId, Guid linkId)
        {
            var vm = _historyService.GetLinkData(linkId);
            if (vm == null)
                return NotFound();
            return View(vm);
        }
    }
}
