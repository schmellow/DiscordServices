using LiteDB;
using Schmellow.DiscordServices.Pinger.Models;
using System.Collections.Generic;
using System.Linq;

namespace Schmellow.DiscordServices.Pinger.Data
{
    public sealed partial class LiteDBStorage : IEventStorage
    {
        private Dictionary<ulong, ILiteCollection<ScheduledEvent>> _eventCollections = new Dictionary<ulong, ILiteCollection<ScheduledEvent>>();

        // Load existing event collections
        private void InitEventStorage()
        {
            foreach(string collectionName in _db.GetCollectionNames())
            {
                if (collectionName.StartsWith("events") == false)
                    continue;
                ulong guildId = ulong.Parse(collectionName.Replace("events", ""));
                var collection = _db.GetCollection<ScheduledEvent>(collectionName);
                collection.EnsureIndex("State");
                _eventCollections[guildId] = collection;
            }
        }

        // get or create event collection
        private ILiteCollection<ScheduledEvent> EnsureEventCollection(ulong guildId)
        {
            ILiteCollection<ScheduledEvent> collection;
            if (!_eventCollections.TryGetValue(guildId, out collection))
            {
                collection = _db.GetCollection<ScheduledEvent>("events" + guildId);
                collection.EnsureIndex("State");
                _eventCollections[guildId] = collection;
            }
            return collection;
        }

        public int InsertEvent(ulong guildId, ScheduledEvent scheduledEvent)
        {
            ILiteCollection<ScheduledEvent> collection = EnsureEventCollection(guildId);
            int id = collection.Insert(scheduledEvent);
            if(id > 0)
                _db.Checkpoint();
            return id;
        }

        public bool UpdateEvent(ulong guildId, ScheduledEvent scheduledEvent)
        {
            ILiteCollection<ScheduledEvent> collection = EnsureEventCollection(guildId);
            var isSuccess = collection.Update(scheduledEvent);
            if(isSuccess)
                _db.Checkpoint();
            return isSuccess;
        }

        public ScheduledEvent GetEventById(ulong guildId, int eventId)
        {
            ILiteCollection<ScheduledEvent> collection = EnsureEventCollection(guildId);
            return collection.FindById(eventId);
        }

        public ScheduledEvent[] FindGuildEvents(
            ulong guildId, 
            string message = null, 
            EventState? state = null, 
            int limit = 0)
        {
            ILiteCollection<ScheduledEvent> collection = EnsureEventCollection(guildId);
            return FindInternal(collection, message, state, limit);            
        }

        public Dictionary<ulong, ScheduledEvent[]> FindAllEvents(
            string message = null, 
            EventState? state = null, 
            int limit = 0)
        {
            var result = new Dictionary<ulong, ScheduledEvent[]>();
            foreach (var kv in _eventCollections)
            {
                var events = FindInternal(kv.Value, message, state, limit);
                if (events.Any())
                    result[kv.Key] = events;
            }
            return result;
        }

        private ScheduledEvent[] FindInternal(
            ILiteCollection<ScheduledEvent> collection,
            string message = null,
            EventState? state = null,
            int limit = 0)
        {
            IEnumerable<ScheduledEvent> events;
            if (state.HasValue)
                events = collection.Find(Query.EQ("State", state.Value.ToString()));
            else
                events = collection.FindAll();
            events = events.OrderBy(e => e.Id);
            if (limit > 0)
                events = events.TakeLast(limit);
            if (!string.IsNullOrEmpty(message))
            {
                message = message.ToLowerInvariant();
                events = events.Where(e => e.Message.ToLowerInvariant().Contains(message));
            }
            return events.ToArray();
        }
    }
}
