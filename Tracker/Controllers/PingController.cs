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

        [HttpGet("{linkId:guid}")]
        public IActionResult ViewPing(Guid linkId)
        {
            Link link = _pingService.GetLink(linkId);
            if (link == null)
                return NotFound();
            Ping ping = _pingService.GetPing(link);
            if (ping == null)
                return NotFound();
            if (!_pingService.RegisterView(link, RequestIp, UserAgent))
                return StatusCode(500);
            return View(ping);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Token")]
        public Dictionary<string, string> CreatePing([FromBody] PingRequest request)
        {
            return _pingService.CreatePing(request);
        }

        [HttpPost("{linkId:guid}")]
        public IActionResult RegisterAction(Guid linkId, [FromBody] string data)
        {
            Link link = _pingService.GetLink(linkId);
            if (link == null)
                return NotFound();
            if (!_pingService.RegisterAction(link, RequestIp, UserAgent, data))
                return StatusCode(500);
            return Ok();
        }

        private string RequestIp
        {
            get
            {
                return Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            }
        }

        private string UserAgent
        {
            get
            {
                return Request.Headers["User-Agent"].ToString();
            }
        }
    }
}
