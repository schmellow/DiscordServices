using Discord;

namespace Schmellow.DiscordServices.Pinger.Models
{
    public sealed class PrivateMessage
    {
        public IUser User { get; private set; }
        public string Message { get; private set; }

        public PrivateMessage(IUser user, string message)
        {
            User = user;
            Message = message;
        }
    }
}
