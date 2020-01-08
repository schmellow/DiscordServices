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
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
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
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowElevatedUsers()
        {
            await ShowProperty(Constants.PROP_ELEVATED_USERS);
        }

        [Command("set-elevated-users")]
        [Summary("Sets users who besides server admin can control bot parameters")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task SetElevatedUsers(params IUser[] users)
        {
            await SetProperty(
                Constants.PROP_ELEVATED_USERS,
                users.Select(u => u.Username + "#" + u.Discriminator).ToArray());
        }

        [Command("show-control-channels")]
        [Summary("Shows channels to which bot control is restricted. Empty value means no restriction")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowControlChannels()
        {
            await ShowProperty(Constants.PROP_CONTROL_CHANNELS);
        }

        [Command("set-control-channels")]
        [Summary("Sets channels to which bot control is restricted. Empty value means no restriction")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task SetControlChannels(params IMessageChannel[] channels)
        {
            await SetProperty(
                Constants.PROP_CONTROL_CHANNELS,
                channels.Select(c => c.Name).ToArray());
        }

        [Command("show-default-ping-channel")]
        [Summary("Shows the default ping channel")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowDefaultPingChannel()
        {
            await ShowProperty(Constants.PROP_DEFAULT_PING_CHANNEL);
        }

        [Command("set-default-ping-channel")]
        [Summary("Sets the default ping channel")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task SetDefaultPingChannel(IMessageChannel channel = null)
        {
            await SetProperty(
                Constants.PROP_DEFAULT_PING_CHANNEL,
                channel == null ? string.Empty : channel.Name);
        }

        [Command("show-ping-users")]
        [Summary("Shows users who have right to ping")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowPingUsers()
        {
            await ShowProperty(Constants.PROP_PING_USERS);
        }

        [Command("set-ping-users")]
        [Summary("Sets users who have right to ping")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task SetPingUsers(params IUser[] users)
        {
            await SetProperty(
                Constants.PROP_PING_USERS,
                users.Select(u => u.Username + "#" + u.Discriminator).ToArray());
        }

        [Command("show-ping-spoofing")]
        [Summary("Show ping spoofing mode")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowPingSpoofing()
        {
            await ShowProperty(Constants.PROP_PING_SPOOFING);
        }

        [Command("set-ping-spoofing")]
        [Summary("Show ping spoofing mode. 'on' == enabled, anything else - disabled")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(Constants.PROP_CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(Constants.PROP_ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task SetPingSpoofing(string value = "")
        {
            await SetProperty(Constants.PROP_PING_SPOOFING, value);
        }

        async Task ShowProperty(string propertyName)
        {
            var value = FormatValue(propertyName);
            if (string.IsNullOrEmpty(value))
            {
                await ReplyAsync("Property is not set");
            }
            else
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.AddField(propertyName, value);
                await ReplyAsync(string.Empty, false, embedBuilder.Build());
            }
        }

        async Task SetProperty(string propertyName, params string[] value)
        {
            var oldValue = FormatValue(propertyName);
            _storage.SetProperty(propertyName, value);
            var newValue = FormatValue(propertyName);
            _logger.Info("Set {0}: '{1}' => '{2}'", propertyName, oldValue, newValue);
            await ReplyAsync("Ok");
        }

        string FormatValue(string propertyName)
        {
            return string.Join(",", _storage.GetProperty(propertyName));
        }
    }
}
