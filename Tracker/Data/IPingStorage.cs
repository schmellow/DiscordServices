using Schmellow.DiscordServices.Tracker.Models;
using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Data
{
    public interface IPingStorage
    {
        int CreatePing(string guild, string author, string text, IEnumerable<string> users);
        int CreateAction(Link link, string origin, string useragent, string data);

        Ping GetPing(int pingId);
        List<Ping> GetPings(int offset = 0, int limit = 0, HashSet<string> guildFilters = null);
        int GetPingCount(HashSet<string> guildFilters = null);
        
        Link GetLink(Guid linkId);
        List<Link> GetLinks(int pingId);
        
        LinkAction GetAction(int actionId);
        List<LinkAction> GetActions(int pingId);
        List<LinkAction> GetActions(Guid linkId);
    }
}
