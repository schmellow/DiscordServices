using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Tracker.Data;
using Schmellow.DiscordServices.Tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

        // Create ping and ping links, return user->url map
        public Dictionary<string, string> CreatePing(PingRequest request)
        {
            if (request == null)
            {
                _logger.LogError("Null request");
                return null;
            }

            int pingId = _pingStorage.CreatePing(
                request.Guild,
                request.Author,
                request.Text,
                request.Users);

            return _pingStorage
                .GetLinks(pingId)
                .ToDictionary(
                    link => link.User, 
                    link => link.Id.ToString().ToLowerInvariant().Replace("-", ""));
        }

        public ViewPingVM GetPingData(Guid linkId)
        {
            // Get link
            Link link = _pingStorage.GetLink(linkId);
            if (link == null)
            {
                _logger.LogError("Unable to find link {0}", linkId);
                return null;
            }
            // Get ping
            Ping ping = _pingStorage.GetPing(link.PingId);
            if (ping == null)
            {
                _logger.LogError("Unable to find ping {0} for link {1}", link.PingId, link.Id);
                return null;
            }
            // Prepare ping
            int i = ping.Author.IndexOf('#');
            string author = i > 0 ? ping.Author.Substring(0, i) : ping.Author;
            string created = ping.Created.ToUniversalTime().ToString("dd.MM.yyyy HH:mm");
            string text = Regex.Replace(
                ping.Text,
                @"((www\.|(http|https|ftp|news|file)+\:\/\/)[&#95;.a-z0-9-]+\.[a-z0-9\/&#95;:@=.+?,##%&~-]*[^.|\'|\# |!|\(|?|,| |>|<|;|\)])",
                "<a href='$1' target='_blank'>$1</a>",
                RegexOptions.IgnoreCase)
                .Replace("href='www", "href='http://www")
                .Replace("\n", "<br/>");
            //
            return new ViewPingVM()
            {
                Author = author,
                Created = created,
                Text = text
            };
        }

        public bool RegisterAction(Guid linkId, string origin, string userAgent, string data)
        {
            // Get link
            Link link = _pingStorage.GetLink(linkId);
            if (link == null)
            {
                _logger.LogError("Unable to find link {0}", linkId);
                return false;
            }
            return _pingStorage.CreateAction(link, origin, userAgent, data) != 0;
        }

    }
}
