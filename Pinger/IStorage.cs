using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Pinger
{
    public interface IStorage : IDisposable
    {
        HashSet<string> GetProperty(string id);
        void SetProperty(string id, HashSet<string> values);
        void SetProperty(string id, params string[] values);
        HashSet<string> GetPropertyNames();
        // TODO: Scheduling storage
        // Add event
        // List events
        // Get event by id
        // Remove event
    }
}
