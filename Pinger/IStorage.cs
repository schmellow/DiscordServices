using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Pinger
{
    public interface IStorage : IDisposable
    {
        string GetProperty(string property);
        void SetProperty(string property, string value);
        // TODO: Scheduling storage
        // Add event
        // List events
        // Get event by id
        // Remove event
    }
}
