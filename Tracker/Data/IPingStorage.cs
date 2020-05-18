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
        Ping[] GetPings(HashSet<string> guilds = null);

        Link GetLink(Guid linkId);
        Link[] GetLinks(params int[] pingIds);
        Link[] GetLinks(string user = null, HashSet<string> guilds = null);

        LinkAction GetAction(int actionId);
        LinkAction[] GetActions(params int[] pingIds);
        LinkAction[] GetActions(params Guid[] linkIds);
        LinkAction[] GetActions(string user = null, HashSet<string> guilds = null);
    }
}
