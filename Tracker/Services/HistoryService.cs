using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Tracker.Data;
using Schmellow.DiscordServices.Tracker.Models;
using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Services
{
    public class HistoryService
    {
        private readonly ILogger<PingService> _logger;
        private readonly IPingStorage _pingStorage;

        public HistoryService(ILogger<PingService> logger, IPingStorage pingStorage)
        {
            _logger = logger;
            _pingStorage = pingStorage;
        }

        public HistoryIndexVM GetIndexData(int page)
        {
            var pings = _pingStorage.GetPings((page - 1) * 20, 20);
            var result = new HistoryIndexVM()
            {
                PageNum = page,
                TotalPages = (_pingStorage.GetPingCount() + 19) / 20
            };
            foreach (Ping ping in pings)
            {
                var links = _pingStorage.GetLinks(ping.Id);
                var actions = _pingStorage.GetActions(ping.Id);
                int userCount = links.Count;
                int actionCount = 0;
                int viewCount = 0;
                int otherCount = 0;
                foreach (LinkAction action in actions)
                {
                    actionCount++;
                    if (action.IsView)
                        viewCount++;
                    else
                        otherCount++;
                }
                result.Pings.Add(new HistoryIndexVM.HistoryPing()
                {
                    Id = ping.Id,
                    Created = ping.Created,
                    Guild = ping.Guild,
                    Author = ping.Author,
                    Text = ping.Text,
                    UserCount = userCount,
                    ActionCount = actionCount,
                    ViewsCount = viewCount,
                    OtherCount = otherCount,                    
                });
            }
            return result;
        }

        public HistoryPingVM GetPingData(int pingId, int parentPage)
        {
            var ping = _pingStorage.GetPing(pingId);
            if (ping == null)
                return null;
            var links = _pingStorage.GetLinks(ping.Id);
            int userCount = links.Count;
            var result = new HistoryPingVM()
            {
                Id = ping.Id,
                Created = ping.Created,
                Guild = ping.Guild,
                Author = ping.Author,
                Text = ping.Text,
                UserCount = links.Count,
                ParentPage = parentPage
            };
            var actions = _pingStorage.GetActions(ping.Id);
            Dictionary<Guid, int> linkActionCount = new Dictionary<Guid, int>();
            Dictionary<Guid, int> linkViewCounts = new Dictionary<Guid, int>();
            Dictionary<Guid, int> linkOtherCounts = new Dictionary<Guid, int>();
            foreach (Link link in links)
            {
                linkActionCount[link.Id] = 0;
                linkViewCounts[link.Id] = 0;
                linkOtherCounts[link.Id] = 0;
            }
            int actionCount = 0;
            int viewCount = 0;
            int otherCount = 0;
            foreach (LinkAction action in actions)
            {
                actionCount++;
                linkActionCount[action.LinkId]++;
                if (action.IsView)
                {
                    viewCount++;
                    linkViewCounts[action.LinkId]++;
                }
                else
                {
                    otherCount++;
                    linkOtherCounts[action.LinkId]++;
                }
            }
            result.ActionCount = actionCount;
            result.ViewsCount = viewCount;
            result.OtherCount = otherCount;
            foreach (Link link in links)
            {
                result.Links.Add(new HistoryPingVM.HistoryLink()
                {
                    Id = link.Id,
                    User = link.User,
                    ActionCount = linkActionCount[link.Id],
                    ViewsCount = linkViewCounts[link.Id],
                    OtherCount = linkOtherCounts[link.Id]
                });
            }
            return result;
        }

        public HistoryLinkVM GetLinkData(Guid linkId)
        {
            var link = _pingStorage.GetLink(linkId);
            if (link == null)
                return null;
            var ping = _pingStorage.GetPing(link.PingId);
            if (ping == null)
                return null;
            var result = new HistoryLinkVM()
            {
                Id = link.Id,
                PingId = link.PingId,
                PingCreated = ping.Created,
                PingGuild = ping.Guild,
                PingAuthor = ping.Author,
                PingText = ping.Text,
                User = link.User,
                ActionCount = 0,
                ViewsCount = 0,
                OtherCount = 0
            };
            var actions = _pingStorage.GetActions(link.Id);
            foreach (LinkAction action in actions)
            {
                result.Actions.Add(action);
                result.ActionCount++;
                if (action.IsView)
                    result.ViewsCount++;
                else
                    result.OtherCount++;
            }
            return result;
        }
    }
}
