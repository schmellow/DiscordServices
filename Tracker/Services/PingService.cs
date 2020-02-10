using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Tracker.Data;
using Schmellow.DiscordServices.Tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Schmellow.DiscordServices.Tracker.Services
{
    public class PingService
    {
        private readonly ILogger<PingService> _logger;
        private readonly IPingStorage _pingStorage;

        public PingService(ILogger<PingService> logger, IPingStorage pingStorage)
        {
            _logger = logger;
            _pingStorage = pingStorage;
        }

        public Link GetLink(Guid linkId)
        {
            Link link = _pingStorage.GetLink(linkId);
            if (link == null)
                _logger.LogError("Unable to find link {0}", linkId);
            return link;
        }

        public Ping GetPing(Link link)
        {
            Ping ping = _pingStorage.GetPing(link.PingId);
            if(ping == null)
                _logger.LogError("Unable to find ping {0} for link {1}", link.PingId, link.Id);
            return ping;
        }

        // Create ping and ping links, return user->url map
        public Dictionary<string, string> CreatePing(PingRequest request)
        {
            if (request == null)
            {
                _logger.LogError("Null request");
                return null;
            }

            int pingId = _pingStorage.CreatePing(request.Guild, request.Author, request.Text, request.Users);
            return _pingStorage.GetLinks(pingId).ToDictionary(
                link => link.User,
                link => link.Id.ToShortString());
        }

        public bool RegisterView(Link link, string origin, string useragent)
        {
            return _pingStorage.CreateAction(link, origin, useragent, "view") != 0;
        }

        public bool RegisterAction(Link link, string origin, string useragent, string data)
        {
            if (data == "view")
            {
                _logger.LogCritical("Attempt to spoof views");
                return false;
            }
            return _pingStorage.CreateAction(link, origin, useragent, data) != 0;
        }

    }
}
