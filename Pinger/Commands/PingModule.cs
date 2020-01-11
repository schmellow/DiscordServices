using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class PingResult : RuntimeResult
    {
        public PingResult(CommandError? error, string reason) : base(error, reason)
        {
        }

        public static PingResult FromError(string reason) => new PingResult(CommandError.Unsuccessful, reason);
        public static PingResult FromSuccess(string reason = null) => new PingResult(null, reason);
    }

    public sealed class PingModule : ModuleBase
    {
        ILogger _logger;
        IStorage _storage;

        public PingModule(ILogger logger, IStorage storage)
        {
            _logger = logger;
            _storage = storage;
        }

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("Posts a ping into default ping channel")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel(BotProperties.CONTROL_CHANNELS)]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser(BotProperties.ELEVATED_USERS, Group = "Access")]
        [RequireUser(BotProperties.PING_USERS, Group = "Access")]
        public async Task<RuntimeResult> Ping([Remainder] string message)
        {
            var defaultChannelName = _storage.GetProperty(BotProperties.DEFAULT_PING_CHANNEL);
            if (string.IsNullOrEmpty(defaultChannelName))
                return PingResult.FromError("Default ping channel is not set");
            var guildChannels = await Context.Guild.GetChannelsAsync();
            IMessageChannel channel = guildChannels
                .Where(c => c is IMessageChannel && c.Name == defaultChannelName)
                .FirstOrDefault() as IMessageChannel;
            if (channel == null)
                return PingResult.FromError(string.Format("Default ping channel '{0}' was not found", defaultChannelName));
            var result = await PingInternal(channel, message);
            return result;
        }

        [Command("ping-channel", RunMode = RunMode.Async)]
        [Summary("Posts a ping into specified channel")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel(BotProperties.CONTROL_CHANNELS)]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser(BotProperties.ELEVATED_USERS, Group = "Access")]
        [RequireUser(BotProperties.PING_USERS, Group = "Access")]
        public async Task<RuntimeResult> PingChannel(IMessageChannel channel, [Remainder] string message)
        {
            var result = await PingInternal(channel, message);
            return result;
        }

        private async Task<RuntimeResult> PingInternal(IMessageChannel channel, string message)
        {
            _logger.Info("Pinging channel '{0}' with message '{1}'", channel.Name, message);
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.AddField(Context.User.Username, message);
            Embed embed = embedBuilder.Build();
            if (IsSpoofingOn)
            {
                var sentMessage = await channel.SendMessageAsync("@everyone\n");
                await Task.Delay(SpoofingDelay);
                await sentMessage.ModifyAsync(m => m.Embed = embed);
            }
            else
            {
                await channel.SendMessageAsync("@everyone\n", false, embed);
            }
            return PingResult.FromSuccess();
        }

        private bool IsSpoofingOn
        { 
            get
            {
                var value = _storage.GetProperty(BotProperties.PING_SPOOFING);
                value = value == null ? "" : value.ToLowerInvariant();
                return value == "on" || value == "yes" || value == "true" || value == "1" || value == "enabled";
            }
        }

        private int SpoofingDelay
        {
            get
            {
                string delayValue = _storage.GetProperty(BotProperties.SPOOFING_DELAY);
                if (!string.IsNullOrEmpty(delayValue))
                {
                    int delay;
                    if (int.TryParse(delayValue, out delay))
                        return delay * 1000;
                }
                return 0;
            }
        }

    }
}
