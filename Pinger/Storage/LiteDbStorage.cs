using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Schmellow.DiscordServices.Pinger.Storage
{
    public sealed class LiteDbStorage : IStorage
    {
        LiteDatabase _db = null;

        public LiteDbStorage(string instanceName)
        {
            bool firstTime = !File.Exists(instanceName + ".db");
            _db = new LiteDatabase(instanceName + ".db");
            if(firstTime)
            {
                foreach (string property in BotProperties.ALL_PROPERTIES.Keys)
                    SetProperty(property, "");
            }
        }

        public string GetProperty(string property)
        {
            var properties = _db.GetCollection<StoredProperty>("configuration");
            return properties.FindById(property)?.Value;
        }

        public void SetProperty(string property, string value)
        {
            if (value == null)
                value = "";
            var properties = _db.GetCollection<StoredProperty>("configuration");
            StoredProperty storedProperty = properties.FindById(property);
            if (storedProperty == null)
            {
                properties.Insert(new StoredProperty()
                {
                    Id = property,
                    Value = value
                });
            }
            else
            {
                storedProperty.Value = value;
                properties.Update(storedProperty);
            }
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
