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
        private static readonly int PING_TEXT_LEN = 256;

        private readonly ILogger<PingService> _logger;
        private readonly IPingStorage _pingStorage;

        public HistoryService(ILogger<PingService> logger, IPingStorage pingStorage)
        {
            _logger = logger;
            _pingStorage = pingStorage;
        }

        #region Pings perspective
        public PingsVM GetPingsData(User viewer)
        {
            // load pings
            Ping[] pings = _pingStorage.GetPings(viewer?.RestrictedServers);
            int[] pingIds = pings.Select(p => p.Id).ToArray();
            // load and map links
            Link[] links = _pingStorage.GetLinks(pingIds);
            Dictionary<int, List<Link>> linkMap = new Dictionary<int, List<Link>>();
            foreach (Link link in links)
            {
                List<Link> list;
                if (!linkMap.TryGetValue(link.PingId, out list))
                {
                    list = new List<Link>();
                    linkMap[link.PingId] = list;
                }
                list.Add(link);
            }
            // load and map actions
            LinkAction[] actions = _pingStorage.GetActions(pingIds);
            Dictionary<Guid, List<LinkAction>> actionMap = new Dictionary<Guid, List<LinkAction>>();
            foreach (LinkAction action in actions)
            {
                List<LinkAction> list;
                if (!actionMap.TryGetValue(action.LinkId, out list))
                {
                    list = new List<LinkAction>();
                    actionMap[action.LinkId] = list;
                }
                list.Add(action);
            }
            // Calculate result
            var result = new PingsVM();
            foreach (Ping ping in pings)
            {
                // prepare text
                string text;
                if (ping.Text.Length > PING_TEXT_LEN)
                    text = ping.Text.Substring(0, PING_TEXT_LEN - 3).Trim() + "...";
                else
                    text = ping.Text;
                text = text.Replace("\n", "<br/>");

                // prepare counters and analyze for suspicious actions
                bool suspicious = false;
                int viewCount = 0;

                List<Link> pingLinks;
                if (!linkMap.TryGetValue(ping.Id, out pingLinks))
                    pingLinks = new List<Link>();
                foreach (Link link in pingLinks)
                {
                    List<LinkAction> linkActions;
                    if (!actionMap.TryGetValue(link.Id, out linkActions))
                        linkActions = new List<LinkAction>();

                    HashSet<string> origins = new HashSet<string>();
                    foreach (LinkAction action in linkActions)
                    {
                        origins.Add(action.Origin);
                        if (IsActionView(action))
                            viewCount++;
                    }
                    if (origins.Count > 1)
                        suspicious = true;
                }
                // Create ping data
                result.Pings.Add(new PingsVM.Ping()
                {
                    Id = ping.Id,
                    Created = ping.Created.ToUniversalTime().ToString("dd.MM.yyyy HH:mm"),
                    Author = ping.Author,
                    Text = text,
                    UserCount = pingLinks.Count,
                    ViewsCount = viewCount,
                    HasSuspiciousActions = suspicious
                });
            }
            return result;
        }

        public PingLinksVM GetPingLinksData(int pingId, User viewer)
        {
            // Load ping
            Ping ping = _pingStorage.GetPing(pingId);
            if (ping == null)
                return null;
            // Check viewer access
            if(viewer != null &&
                viewer.RestrictedServers != null &&
                viewer.RestrictedServers.Any() &&
                viewer.RestrictedServers.Contains(ping.Guild))
            {
                _logger.LogWarning(
                    "User {0} is not allowed to browse pings from guild '{1}'",
                    viewer.CharacterName,
                    ping.Guild);
                return null;
            }
            // Load links
            Link[] links = _pingStorage.GetLinks(ping.Id);
            // Load ana map actions
            LinkAction[] actions = _pingStorage.GetActions(ping.Id);
            Dictionary<Guid, List<LinkAction>> actionMap = new Dictionary<Guid, List<LinkAction>>();
            foreach (LinkAction action in actions)
            {
                List<LinkAction> list;
                if (!actionMap.TryGetValue(action.LinkId, out list))
                {
                    list = new List<LinkAction>();
                    actionMap[action.LinkId] = list;
                }
                list.Add(action);
            }
            // Calculate result
            var result = new PingLinksVM()
            {
                PingId = ping.Id,
                PingCreated = ping.Created.ToUniversalTime().ToString("dd.MM.yyyy HH:mm"),
                PingAuthor = ping.Author,
                PingText = ping.Text.Replace("\n", "<br/>")
            };
            foreach (Link link in links)
            {
                List<LinkAction> linkActions;
                if (!actionMap.TryGetValue(link.Id, out linkActions))
                    linkActions = new List<LinkAction>();

                int viewCount = 0;
                bool suspicious = false;
                HashSet<string> origins = new HashSet<string>();
                foreach (LinkAction action in linkActions)
                {
                    origins.Add(action.Origin);
                    if (IsActionView(action))
                        viewCount++;
                }
                if (origins.Count > 1)
                {
                    result.MultipleOriginUsers.Add(link.User, link.Id);
                    suspicious = true;
                }
                result.Links.Add(new PingLinksVM.Link()
                {
                    Id = link.Id,
                    User = link.User,
                    ViewsCount = viewCount,
                    HasSuspiciousActions = suspicious
                });
            }
            return result;
        }

        public PingActionsVM GetPingActionsData(int pingId, User viewer)
        {
            // Load ping
            Ping ping = _pingStorage.GetPing(pingId);
            if (ping == null)
                return null;
            // Check viewer access
            if (viewer != null &&
                viewer.RestrictedServers != null &&
                viewer.RestrictedServers.Any() &&
                viewer.RestrictedServers.Contains(ping.Guild))
            {
                _logger.LogWarning(
                    "User {0} is not allowed to browse pings from guild '{1}'",
                    viewer.CharacterName,
                    ping.Guild);
                return null;
            }
            // Load actions
            LinkAction[] actions = _pingStorage.GetActions(ping.Id);
            // Calculate result
            var result = new PingActionsVM()
            {
                PingId = ping.Id,
                PingCreated = ping.Created.ToUniversalTime().ToString("dd.MM.yyyy HH:mm"),
                PingAuthor = ping.Author,
                PingText = ping.Text.Replace("\n", "<br/>")
            };
            // Create actions and map out multiple-origin actions in one go
            Dictionary<string, HashSet<string>> originMap = new Dictionary<string, HashSet<string>>();
            Dictionary<string, Guid> linkMap = new Dictionary<string, Guid>();
            foreach(LinkAction action in actions)
            {
                result.Actions.Add(new PingActionsVM.Action()
                {
                    LinkId = action.LinkId,
                    User = action.User,
                    When = action.When.ToUniversalTime().ToString("dd.MM.yyyy HH:mm"),
                    Origin = action.Origin,
                    UserAgent = action.UserAgent,
                    Data = action.Data,
                    IsSuspicious = IsActionSuspicious(action)
                });
                // map user -> linkid
                if (!linkMap.ContainsKey(action.User))
                    linkMap[action.User] = action.LinkId;
                // map user -> unique origins
                HashSet<string> originSet;
                if(!originMap.TryGetValue(action.User, out originSet))
                {
                    originSet = new HashSet<string>();
                    originMap[action.User] = originSet;
                }
                originSet.Add(action.Origin);
            }
            // Commit detected multiple origins
            foreach (var kv in originMap.Where(kv => kv.Value.Count > 1))
                result.MultipleOriginUsers[kv.Key] = linkMap[kv.Key];
            
            return result;
        }

        public LinkActionsVM GetLinkActionsData(Guid linkId, User viewer)
        {
            // Load link
            Link link = _pingStorage.GetLink(linkId);
            if (link == null)
                return null;
            // Check viewer access
            if (viewer != null &&
                viewer.RestrictedServers != null &&
                viewer.RestrictedServers.Any() &&
                viewer.RestrictedServers.Contains(link.Guild))
            {
                _logger.LogWarning(
                    "User {0} is not allowed to browse pings from guild '{1}'",
                    viewer.CharacterName,
                    link.Guild);
                return null;
            }
            // Load ping
            Ping ping = _pingStorage.GetPing(link.PingId);
            if (ping == null)
                return null;
            // Load actions
            LinkAction[] actions = _pingStorage.GetActions(link.Id);
            // Calculate result
            var result = new LinkActionsVM()
            {
                PingId = link.PingId,
                LinkId = link.Id,
                PingCreated = ping.Created.ToUniversalTime().ToString("dd.MM.yyyy HH:mm"),
                PingAuthor = ping.Author,
                PingText = ping.Text.Replace("\n", "<br/>"),
                User = link.User
            };
            HashSet<string> origins = new HashSet<string>();
            foreach(LinkAction action in actions)
            {
                result.Actions.Add(new LinkActionsVM.Action()
                {
                    When = action.When.ToUniversalTime().ToString("dd.MM.yyyy HH:mm"),
                    Origin = action.Origin,
                    UserAgent = action.UserAgent,
                    Data = action.Data,
                    IsSuspicious = IsActionSuspicious(action)
                });
                origins.Add(action.Origin);
            }
            result.HasMultipleOrigins = origins.Count > 1;
            //
            return result;
        }
        #endregion

        #region User perspective
        public UsersVM GetUsersData(User viewer)
        {
            // Load links and count links by user
            Link[] links = _pingStorage.GetLinks(guilds: viewer?.RestrictedServers);
            Dictionary<string, int> linkCounts = new Dictionary<string, int>();
            foreach(Link link in links)
            {
                if (!linkCounts.ContainsKey(link.User))
                    linkCounts[link.User] = 0;
                linkCounts[link.User]++;
            }
            // Load actions and map stats
            LinkAction[] actions = _pingStorage.GetActions(guilds: viewer?.RestrictedServers);
            Dictionary<string, int> viewCounts = new Dictionary<string, int>();
            Dictionary<string, bool> suspectMap = new Dictionary<string, bool>();
            Dictionary<string, Dictionary<Guid, HashSet<string>>> originMap = new Dictionary<string, Dictionary<Guid, HashSet<string>>>();
            foreach(LinkAction action in actions)
            {
                if (!viewCounts.ContainsKey(action.User))
                    viewCounts[action.User] = 0;
                if (!suspectMap.ContainsKey(action.User))
                    suspectMap[action.User] = false;
                Dictionary<Guid, HashSet<string>> linkOrigins;
                if (!originMap.TryGetValue(action.User, out linkOrigins))
                {
                    linkOrigins = new Dictionary<Guid, HashSet<string>>();
                    originMap[action.User] = linkOrigins;
                }
                HashSet<string> origins;
                if(!linkOrigins.TryGetValue(action.LinkId, out origins))
                {
                    origins = new HashSet<string>();
                    linkOrigins[action.LinkId] = origins;
                }
                origins.Add(action.Origin);
                if (IsActionView(action))
                    viewCounts[action.User]++;
                if (IsActionSuspicious(action))
                    suspectMap[action.User] = true;
            }
            // Detect multiple origins on per-link basis
            foreach(var kv in originMap)
            {
                if(kv.Value.Any(kv2 => kv2.Value.Count > 1))
                    suspectMap[kv.Key] = true;
            }
            // Create result
            var result = new UsersVM();
            foreach(var kv in linkCounts)
            {
                string user = kv.Key;
                int linkCount = kv.Value;
                int viewCount = viewCounts.GetValueOrDefault(user);
                bool suspicious = suspectMap.GetValueOrDefault(user);
                result.Users.Add(new UsersVM.User()
                {
                    Name = user,
                    LinksCount = linkCount,
                    ViewsCount = viewCount,
                    IsSuspicious = suspicious
                });
            }
            return result;
        }

        public UserActionsVM GetUserActionsData(string user, User viewer)
        {
            // Load actions
            LinkAction[] actions = _pingStorage.GetActions(user, viewer?.RestrictedServers);
            // Calculate results
            var result = new UserActionsVM()
            {
                UserName = user
            };
            Dictionary<Guid, int> pingMap = new Dictionary<Guid, int>();
            Dictionary<Guid, HashSet<string>> originMap = new Dictionary<Guid, HashSet<string>>();
            foreach (LinkAction action in actions)
            {
                result.Actions.Add(new UserActionsVM.Action()
                {
                    PingId = action.PingId,
                    LinkId = action.LinkId,
                    When = action.When.ToUniversalTime().ToString("dd.MM.yyyy HH:mm"),
                    Origin = action.Origin,
                    UserAgent = action.UserAgent,
                    Data = action.Data,
                    IsSuspicious = IsActionSuspicious(action)
                });
                if (!pingMap.ContainsKey(action.LinkId))
                    pingMap[action.LinkId] = action.PingId;
                HashSet<string> origins;
                if(!originMap.TryGetValue(action.LinkId, out origins))
                {
                    origins = new HashSet<string>();
                    originMap[action.LinkId] = origins;
                }
                origins.Add(action.Origin);
            }
            foreach(var kv in originMap)
            {
                if (kv.Value.Count > 1)
                    result.MultipleOriginLinks[pingMap[kv.Key]] = kv.Key;
            }
            return result;
        }
        #endregion

        #region Utility
        private bool IsActionView(LinkAction action)
        {
            return action.Data == "view";
        }

        private bool IsActionSuspicious(LinkAction action)
        {
            var data = action.Data;
            var userAgent = action.UserAgent;
            return data != "view" ||
                userAgent.Contains("discord", StringComparison.InvariantCultureIgnoreCase) ||
                userAgent.Contains("curl", StringComparison.InvariantCultureIgnoreCase) ||
                userAgent.Contains("wget", StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion
    }
}
