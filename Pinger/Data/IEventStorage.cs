using Schmellow.DiscordServices.Pinger.Models;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Pinger.Data
{
    public interface IEventStorage
    {
        int InsertEvent(ulong guildId, ScheduledEvent scheduledEvent);
        bool UpdateEvent(ulong guildId, ScheduledEvent scheduledEvent);
        ScheduledEvent GetEventById(ulong guildId, int eventId);
        ScheduledEvent[] FindGuildEvents(ulong guildId, string message = null, EventState? state = null, int limit = 0);
        Dictionary<ulong, ScheduledEvent[]> FindAllEvents(string message = null, EventState? state = null, int limit = 0);
    }
}
