using System;
using System.Collections.Generic;
using System.Linq;

namespace Schmellow.DiscordServices.Pinger.Models
{
    public sealed class ScheduledPing
    {
        public DateTime Date { get; private set; }
        public Dictionary<ulong, Dictionary<int, ScheduledEvent>> Events { get; private set; }

        public ScheduledPing(DateTime date)
        {
            Date = date;
            Events = new Dictionary<ulong, Dictionary<int, ScheduledEvent>>();
        }

        public ScheduledPing(DateTime date, Dictionary<ulong, Dictionary<int, ScheduledEvent>> events)
        {
            Date = date;
            Events = events;
        }

        public bool IsEmpty
        {
            get
            {
                return !Events.Any();
            }
        }

        public void SetEvent(ulong guildId, ScheduledEvent se)
        {
            Dictionary<int, ScheduledEvent> eventMap;
            if(!Events.TryGetValue(guildId, out eventMap))
            {
                eventMap = new Dictionary<int, ScheduledEvent>();
                Events[guildId] = eventMap;
            }
            eventMap[se.Id] = se;
        }

        public void RemoveEvent(ulong guildId, ScheduledEvent se)
        {
            Dictionary<int, ScheduledEvent> eventMap;
            if(Events.TryGetValue(guildId, out eventMap))
            {
                if(eventMap.ContainsKey(se.Id))
                {
                    eventMap.Remove(se.Id);
                    if (!eventMap.Any())
                        Events.Remove(guildId);
                }
            }
        }

        public bool HasEvent(ulong guildId, ScheduledEvent se)
        {
            Dictionary<int, ScheduledEvent> eventMap;
            if (Events.TryGetValue(guildId, out eventMap))
            {
                return eventMap.ContainsKey(se.Id);
            }
            return false;
        }
    }
}
