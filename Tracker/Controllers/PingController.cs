using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Tracker.Models;
using Schmellow.DiscordServices.Tracker.Services;

namespace Schmellow.DiscordServices.Tracker.Controllers
{
    [Route("")]
    public class PingController : Controller
    {
        private readonly ILogger<PingController> _logger;
        private readonly PingService _pingService;

        public PingController(ILogger<PingController> logger, PingService pingService)
        {
            _logger = logger;
            _pingService = pingService;
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Token")]
        public Dictionary<string, string> CreatePing([FromBody] PingRequest request)
        {
            return _pingService.CreatePing(request);
        }

        [HttpGet("{linkId:guid}")]
        public IActionResult ViewPing(Guid linkId)
        {
            string ip = Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            string userAgent = Request.Headers["User-Agent"].ToString();
            if (!_pingService.RegisterAction(linkId, ip, userAgent, "view"))
                return StatusCode(500);

            var vm = _pingService.GetPingData(linkId);
            if (vm == null)
                return NotFound();

            return View(vm);
        }

        [HttpPost("{linkId:guid}")]
        public IActionResult RegisterAction(Guid linkId, [FromBody] string data)
        {
            if (data == "view")
            {
                _logger.LogCritical("Attempt to spoof views");
                return StatusCode(500);
            }

            string ip = Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            string userAgent = Request.Headers["User-Agent"].ToString();
            return _pingService.RegisterAction(linkId, ip, userAgent, data) ? Ok() : StatusCode(500);
        }
    }
}
