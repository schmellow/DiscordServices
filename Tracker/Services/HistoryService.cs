using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Tracker.Data;
using Schmellow.DiscordServices.Tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Schmellow.DiscordServices.Tracker.Services
{
    public class HistoryService
    {
        private static readonly int PAGE_LIM = 20;
        private readonly ILogger<PingService> _logger;
        private readonly IPingStorage _pingStorage;

        public HistoryService(ILogger<PingService> logger, IPingStorage pingStorage)
        {
            _logger = logger;
            _pingStorage = pingStorage;
        }

        public HistoryIndexVM GetIndexData(int page, User user)
        {
            List<Ping> pings = _pingStorage.GetPings((page - 1) * PAGE_LIM, PAGE_LIM, user?.RestrictedServers);
            var result = new HistoryIndexVM()
            {
                PageNum = page,
                TotalPages = (_pingStorage.GetPingCount(user?.RestrictedServers) + (PAGE_LIM - 1)) / PAGE_LIM
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

        public HistoryPingVM GetPingData(int pingId, User user)
        {
            Ping ping = _pingStorage.GetPing(pingId);
            if (ping == null)
                return null;
            if(user != null && 
               user.RestrictedServers != null && 
               user.RestrictedServers.Any() &&
               user.RestrictedServers.Contains(ping.Guild) == false)
            {
                _logger.LogWarning(
                    "User '{0}' is not allowed to browse pings from guild '{1}'", 
                    user.CharacterName, 
                    ping.Guild);
                return null;
            }
            List<Link> links = _pingStorage.GetLinks(ping.Id);
            var result = new HistoryPingVM()
            {
                Id = ping.Id,
                Created = ping.Created,
                Guild = ping.Guild,
                Author = ping.Author,
                Text = ping.Text,
                UserCount = links.Count
            };
            List<LinkAction> actions = _pingStorage.GetActions(ping.Id);
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

        public HistoryTimelineVM GetTimelineData(int pingId, User user)
        {
            Ping ping = _pingStorage.GetPing(pingId);
            if (ping == null)
                return null;
            if (user != null &&
               user.RestrictedServers != null &&
               user.RestrictedServers.Any() &&
               user.RestrictedServers.Contains(ping.Guild) == false)
            {
                _logger.LogWarning(
                    "User '{0}' is not allowed to browse pings from guild '{1}'",
                    user.CharacterName,
                    ping.Guild);
                return null;
            }
            var result = new HistoryTimelineVM()
            {
                PingId = ping.Id,
                PingCreated = ping.Created,
                PingGuild = ping.Guild,
                PingAuthor = ping.Author,
                PingText = ping.Text,
                ActionCount = 0,
                ViewsCount = 0,
                OtherCount = 0
            };
            List<Link> links = _pingStorage.GetLinks(ping.Id);
            Dictionary<Guid, string> linkIdToUser = links.ToDictionary(l => l.Id, l => l.User);
            List<LinkAction> actions = _pingStorage.GetActions(ping.Id);
            foreach(LinkAction action in actions.OrderByDescending(a => a.When))
            {
                result.ActionCount++;
                if (action.IsView)
                    result.ViewsCount++;
                else
                    result.OtherCount++;
                result.Actions.Add(new HistoryTimelineVM.HistoryAction()
                {
                    User = linkIdToUser[action.LinkId],
                    Action = action
                });
            }
            return result;
        }

        public HistoryLinkVM GetLinkData(Guid linkId, User user)
        {
            Link link = _pingStorage.GetLink(linkId);
            if (link == null)
                return null;
            Ping ping = _pingStorage.GetPing(link.PingId);
            if (ping == null)
                return null;
            if (user != null &&
                user.RestrictedServers != null &&
                user.RestrictedServers.Any() &&
                user.RestrictedServers.Contains(ping.Guild) == false)
            {
                _logger.LogWarning(
                    "User '{0}' is not allowed to browse pings from guild '{1}'", 
                    user.CharacterName, 
                    ping.Guild);
                return null;
            }
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
            List<LinkAction> actions = _pingStorage.GetActions(link.Id);
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
