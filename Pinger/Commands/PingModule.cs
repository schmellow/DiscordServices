using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
        DiscordSocketClient _client;

        public PingModule(ILogger logger, IStorage storage, DiscordSocketClient client)
        {
            _logger = logger;
            _storage = storage;
            _client = client;
        }

        [Command("ping")]
        [Summary("Posts a ping into default ping channel")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task<RuntimeResult> Ping([Remainder] string message)
        {
            _logger.Info("Pinging default channel with message '{0}'", message);
            var pingChannel = _storage.GetProperty(Constants.PROP_DEFAULT_PING_CHANNEL).FirstOrDefault();
            if (string.IsNullOrEmpty(pingChannel))
                return PingResult.FromError("Default ping channel is not set");
            IMessageChannel channel = _client
                    .GetGuild(Context.Guild.Id)
                    .Channels.FirstOrDefault(c => c.Name == pingChannel) as IMessageChannel;
            if (channel == null)
                return PingResult.FromError(string.Format("Default channel '{0}' was not found", pingChannel));
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.AddField("ping", message);
            Embed embed = embedBuilder.Build();
            await channel.SendMessageAsync("@everyone\n", false, embed);
            return PingResult.FromSuccess();
        }

        [Command("ping-channel")]
        [Summary("Posts a ping into specified channel")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task<RuntimeResult> PingChannel(IMessageChannel channel, [Remainder] string message)
        {
            _logger.Info("Pinging channel '{0}' with message '{1}'", channel.Name, message);
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.AddField("ping", message);
            Embed embed = embedBuilder.Build();
            await channel.SendMessageAsync("@everyone\n", false, embed);
            return PingResult.FromSuccess();
        }
    }
}
