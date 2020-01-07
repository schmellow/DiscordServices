using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Schmellow.DiscordServices.Pinger.Storage
{
    public sealed class LiteDbStorage : IStorage
    {
        LiteDatabase _db = null;

        public LiteDbStorage(string instanceName)
        {
            _db = new LiteDatabase(instanceName + ".db");
        }

        public HashSet<string> GetProperty(string id)
        {
            var properties = _db.GetCollection<StoredProperty>("configuration");
            var property = properties.FindById(id);
            return property == null ? new HashSet<string>() : property.Values;
        }

        public void SetProperty(string id, HashSet<string> values)
        {
            var properties = _db.GetCollection<StoredProperty>("configuration");
            var property = properties.FindById(id);
            if(property == null)
            {
                properties.Insert(new StoredProperty()
                {
                    Id = id,
                    Values = values
                });
            }
            else
            {
                property.Values = values;
                properties.Update(property);
            }
        }

        public void SetProperty(string id, params string[] values)
        {
            var properties = _db.GetCollection<StoredProperty>("configuration");
            var property = properties.FindById(id);
            if(property == null)
            {
                properties.Insert(new StoredProperty()
                {
                    Id = id,
                    Values = new HashSet<string>(values)
                });
            }
            else
            {
                property.Values = new HashSet<string>(values);
                properties.Update(property);
            }
        }

        public HashSet<string> GetPropertyNames()
        {
            var properties = _db.GetCollection<StoredProperty>("configuration");
            return new HashSet<string>(properties.FindAll().Select(p => p.Id));
        }

        // TODO: Scheduling storage
        // Add event
        // List events
        // Get event by id
        // Remove event

        public void Dispose()
        {
            if (_db != null)
                _db.Dispose();
        }

    }
}
