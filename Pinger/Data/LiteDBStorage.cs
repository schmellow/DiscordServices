using LiteDB;
using Schmellow.DiscordServices.Pinger.Models;
using System;
using System.IO;

namespace Schmellow.DiscordServices.Pinger.Data
{
    public sealed partial class LiteDBStorage : IDisposable
    {
        private readonly LiteDatabase _db;

        public LiteDBStorage(BotProperties botProperties)
        {
            _db = new LiteDatabase(Path.Combine(
                botProperties.DataDirectory,
                botProperties.InstanceName.ToLowerInvariant() + ".db"));
            BsonMapper.Global.EmptyStringToNull = false;
            InitPropertyStorage();
            InitEventStorage();
        }

        public void Dispose()
        {
            if (_db != null)
                _db.Dispose();
        }
    }
}
