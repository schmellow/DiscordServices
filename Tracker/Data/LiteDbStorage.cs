using LiteDB;
using Schmellow.DiscordServices.Tracker.Models;
using System;
using System.IO;

namespace Schmellow.DiscordServices.Tracker.Data
{
    public sealed partial class LiteDBStorage : IDisposable
    {
        private readonly LiteDatabase _db;

        public LiteDBStorage(TrackerProperties trackerProperties)
        {
            _db = new LiteDatabase(Path.Combine(
                trackerProperties.DataDirectory,
                trackerProperties.InstanceName.ToLowerInvariant() + ".db"));
            BsonMapper.Global.EmptyStringToNull = false;
            InitPingStorage();
            InitAuthStorage();
        }

        public void Dispose()
        {
            if (_db != null)
                _db.Dispose();
        }
    }
}
