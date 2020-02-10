using Discord;
using System.Linq;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public static class DiscordExtensions
    {
        public static async Task<T> GetChannelByName<T>(this IGuild guild, string channelName)
            where T : IChannel
        {               
            var channels = await guild.GetChannelsAsync();
            return channels
                .OfType<T>()
                .Where(c => c.Name == channelName)
                .FirstOrDefault();
        }

        public static async Task<T> GetUserByName<T>(this IGuild guild, string userName)
            where T : IUser
        {
            var users = await guild.GetUsersAsync();
            return users
                .OfType<T>()
                .Where(u => u.Username == userName)
                .FirstOrDefault();
        }
    }
}
