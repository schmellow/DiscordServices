using LiteDB;
using Schmellow.DiscordServices.Pinger.Models;

namespace Schmellow.DiscordServices.Pinger.Data
{
    public sealed partial class LiteDBStorage : IGuildPropertyStorage
    {
        private ILiteCollection<GuildProperties> GuildProperties
        {
            get
            {
                return _db.GetCollection<GuildProperties>("guildproperties");
            }
        }

        private void InitPropertyStorage()
        {
            BsonMapper.Global.Entity<GuildProperties>().Id(p => p.GuildIdString, false);
        }

        public GuildProperties EnsureGuildProperties(ulong guildId)
        {
            GuildProperties guildProperties = GuildProperties.FindById(guildId.ToString());
            if(guildProperties == null)
            {
                guildProperties = new GuildProperties()
                {
                    GuildId = guildId
                };
                string id = GuildProperties.Insert(guildProperties);
                if(!string.IsNullOrEmpty(id))
                    _db.Checkpoint();
            }
            return guildProperties;
        }

        public bool UpdateGuildProperties(GuildProperties guildProperties)
        {
            bool isSuccess = GuildProperties.Update(guildProperties);
            if(isSuccess)
                _db.Checkpoint();
            return isSuccess;
        }
    }
}
