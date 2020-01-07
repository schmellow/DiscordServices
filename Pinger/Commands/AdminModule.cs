using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class AdminModule : ModuleBase
    {
        ILogger _logger;
        IStorage _storage;

        public AdminModule(ILogger logger, IStorage storage)
        {
            _logger = logger;
            _storage = storage;
        }

        [Command("show-properties")]
        [Summary("Shows all bot configuration properties")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowProperties()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            foreach (string propertyName in _storage.GetPropertyNames())
            {
                if (propertyName == Constants.PROP_TOKEN)
                    continue;
                var value = FormatValue(propertyName);
                if (string.IsNullOrEmpty(value))
                    continue;
                embedBuilder.AddField(propertyName, value);
            }
            if (embedBuilder.Fields.Count == 0)
            {
                await ReplyAsync("No properties currently set");
            }
            else
            {
                await ReplyAsync(string.Empty, false, embedBuilder.Build());
            }
        }

        [Command("show-elevated-users")]
        [Summary("Shows users who besides server admin can control bot parameters")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowElevatedUsers()
        {
            var value = FormatValue(Constants.PROP_ELEVATED_USERS);
            if (string.IsNullOrEmpty(value))
            {
                await ReplyAsync("Property is not set");
            }
            else
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.AddField(Constants.PROP_ELEVATED_USERS, value);
                await ReplyAsync(string.Empty, false, embedBuilder.Build());
            }
        }

        [Command("set-elevated-users")]
        [Summary("Sets users who besides server admin can control bot parameters")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task SetElevatedUsers(params IUser[] users)
        {
            var oldValue = FormatValue(Constants.PROP_ELEVATED_USERS);
            var names = users.Select(u => u.Username).ToArray();
            _storage.SetProperty(Constants.PROP_ELEVATED_USERS, names);
            var newValue = FormatValue(Constants.PROP_ELEVATED_USERS);
            _logger.Info("Set {0}: '{1}' => '{2}'", Constants.PROP_ELEVATED_USERS, oldValue, newValue);
            await ReplyAsync("Ok");
        }

        [Command("show-control-channels")]
        [Summary("Shows channels to which bot control is restricted. Empty value means no restriction")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowControlChannels()
        {
            var value = FormatValue(Constants.PROP_CONTROL_CHANNELS);
            if (string.IsNullOrEmpty(value))
            {
                await ReplyAsync("Property is not set");
            }
            else
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.AddField(Constants.PROP_CONTROL_CHANNELS, FormatValue(Constants.PROP_CONTROL_CHANNELS));
                await ReplyAsync(string.Empty, false, embedBuilder.Build());
            }
        }

        [Command("set-control-channels")]
        [Summary("Sets channels to which bot control is restricted. Empty value means no restriction")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task SetControlChannels(params IMessageChannel[] channels)
        {
            var oldValue = FormatValue(Constants.PROP_CONTROL_CHANNELS);
            var names = channels.Select(c => c.Name).ToArray();
            _storage.SetProperty(Constants.PROP_CONTROL_CHANNELS, names);
            var newValue = FormatValue(Constants.PROP_CONTROL_CHANNELS);
            _logger.Info("Set {0}: '{1}' => '{2}'", Constants.PROP_CONTROL_CHANNELS, oldValue, newValue);
            await ReplyAsync("Ok");
        }

        [Command("show-default-ping-channel")]
        [Summary("Shows the default ping channel")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowDefaultPingChannel()
        {
            var value = FormatValue(Constants.PROP_DEFAULT_PING_CHANNEL);
            if (string.IsNullOrEmpty(value))
            {
                await ReplyAsync("Property is not set");
            }
            else
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.AddField(Constants.PROP_DEFAULT_PING_CHANNEL, value);
                await ReplyAsync(string.Empty, false, embedBuilder.Build());
            }
        }

        [Command("set-default-ping-channel")]
        [Summary("Sets the default ping channel")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task SetDefaultPingChannel(IMessageChannel channel = null)
        {
            var oldValue = FormatValue(Constants.PROP_DEFAULT_PING_CHANNEL);
            _storage.SetProperty(Constants.PROP_DEFAULT_PING_CHANNEL, channel == null ? string.Empty : channel.Name);
            var newValue = FormatValue(Constants.PROP_DEFAULT_PING_CHANNEL);
            _logger.Info("Set {0}: '{1}' => '{2}'", Constants.PROP_DEFAULT_PING_CHANNEL, oldValue, newValue);
            await ReplyAsync("Ok");
        }

        string FormatValue(string propertyName)
        {
            return string.Join(",", _storage.GetProperty(propertyName));
        }
    }
}
