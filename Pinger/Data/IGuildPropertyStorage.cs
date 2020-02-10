using Schmellow.DiscordServices.Pinger.Models;

namespace Schmellow.DiscordServices.Pinger.Data
{
    public interface IGuildPropertyStorage
    {
        GuildProperties EnsureGuildProperties(ulong guildId);
        bool UpdateGuildProperties(GuildProperties properties);
    }
}
