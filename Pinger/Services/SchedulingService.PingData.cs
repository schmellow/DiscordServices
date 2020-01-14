using System;
using System.Collections.Generic;
using System.Linq;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public sealed partial class SchedulingService
    {
        public sealed class PingData
        {
            public DateTime Date { get; private set; }
            public Dictionary<ulong, HashSet<int>> EventMap { get; private set; }

            public PingData(DateTime date)
            {
                Date = date;
                EventMap = new Dictionary<ulong, HashSet<int>>();
            }

            public PingData(DateTime date, Dictionary<ulong, HashSet<int>> eventMap)
            {
                Date = date;
                EventMap = eventMap;
            }

            public bool IsEmpty
            {
                get
                {
                    return EventMap.Any() == false;
                }
            }

            public void AddEvent(ulong guildId, int eventId)
            {
                HashSet<int> eventIds;
                if(!EventMap.TryGetValue(guildId, out eventIds))
                {
                    eventIds = new HashSet<int>();
                    EventMap[guildId] = eventIds;
                }
                eventIds.Add(eventId);
            }

            public void RemoveEvent(ulong guildId, int eventId)
            {
                HashSet<int> eventIds;
                if (!EventMap.TryGetValue(guildId, out eventIds))
                    return;
                eventIds.Remove(eventId);
                if (!eventIds.Any())
                    EventMap.Remove(guildId);
            }

            public bool HasEvent(ulong guildId, int eventId)
            {
                HashSet<int> eventIds;
                if (!EventMap.TryGetValue(guildId, out eventIds))
                    return false;
                return eventIds.Contains(eventId);
            }
        }
    }
}
