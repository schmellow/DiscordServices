using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        [Command("show-property")]
        [Summary("Shows value of the property")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(BotProperties.CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(BotProperties.ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ShowProperty([Remainder] string property)
        {
            if (BotProperties.ExistsAndUnrestricted(property))
            {
                string value = _storage.GetProperty(property);
                if(string.IsNullOrEmpty(value))
                {
                    await ReplyAsync(string.Format("Property '{0}' is not set", property));
                }
                else
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.AddField(property, value);
                    await ReplyAsync(string.Empty, false, embedBuilder.Build());
                }
            }
            else
            {
                await ReplyAsync(string.Format("Property '{0}' does not exist", property));
            }
        }

        [Command("set-property")]
        [Summary("Sets property")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(BotProperties.CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(BotProperties.ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task SetProperty(string property, params string[] values)
        {
            if (BotProperties.ExistsAndUnrestricted(property))
            {
                if(values == null || values.Length == 0)
                {
                    await SetString(property, string.Empty);
                }
                else
                {
                    var type = BotProperties.ALL_PROPERTIES[property].Type;
                    switch (type)
                    {
                        case BotProperties.PropertyType.Users:
                            await SetUsers(property, false, values);
                            break;
                        case BotProperties.PropertyType.Channels:
                            await SetChannels(property, false, values);
                            break;
                        case BotProperties.PropertyType.User:
                            await SetUsers(property, true, values);
                            break;
                        case BotProperties.PropertyType.Channel:
                            await SetChannels(property, true, values);
                            break;
                        default:
                            await SetString(property, string.Join(" ", values));
                            break;
                    }
                }
            }
            else
            {
                await ReplyAsync(string.Format("Property '{0}' does not exist", property));
            }
        }

        private async Task SetUsers(string property, bool single, params string[] values)
        {
            // load users
            var idRegex = new Regex(@"\d+");
            var users = new List<IGuildUser>();
            foreach (string value in values)
            {
                Match match = idRegex.Match(value);
                if (match.Success)
                {
                    ulong id;
                    if (ulong.TryParse(match.Value, out id))
                    {
                        var user = await Context.Guild.GetUserAsync(id);
                        if (user != null)
                            users.Add(user);
                    }
                        
                }
            }
            // set users
            if(users.Count == 0)
            {
                await ReplyAsync("No users found");
            }
            else
            {
                if(single)
                {
                    if(users.Count > 1)
                    {
                        await ReplyAsync(string.Format("Expected single user, got {0}", users.Count));
                    }
                    else
                    {
                        await SetString(property, users[0].Username + "#" + users[0].Discriminator);
                    }
                }
                else 
                {
                    await SetString(
                        property,
                        string.Join("|", users.Select(u => u.Username + "#" + u.Discriminator)) + "|");
                }
            }
        }

        private async Task SetChannels(string property, bool single, params string[] values)
        {
            // load channels
            var idRegex = new Regex(@"\d+");
            var channels = new List<IGuildChannel>();
            foreach (string value in values)
            {
                Match match = idRegex.Match(value);
                if (match.Success)
                {
                    ulong id;
                    if (ulong.TryParse(match.Value, out id))
                    {
                        var channel = await Context.Guild.GetChannelAsync(id);
                        if (channel != null)
                            channels.Add(channel);
                    }

                }
            }
            // set channels
            if (channels.Count == 0)
            {
                await ReplyAsync("No channels found");
            }
            else
            {
                if (single)
                {
                    if (channels.Count > 1)
                    {
                        await ReplyAsync(string.Format("Expected single channel, got {0}", channels.Count));
                    }
                    else
                    {
                        await SetString(property, channels[0].Name);
                    }
                }
                else
                {
                    await SetString(
                        property,
                        string.Join("|", channels.Select(c => c.Name)) + "|");
                }
            }
        }

        private async Task SetString(string property, string value)
        {
            var oldValue = _storage.GetProperty(property);
            _storage.SetProperty(property, value);
            _logger.Info("Set {0}: '{1}' => '{2}'", property, oldValue, value);
            await ReplyAsync("Ok");
        }

        [Command("list-properties")]
        [Summary("Lists available properties")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireChannel(BotProperties.CONTROL_CHANNELS, ErrorMessage = Constants.ERROR_DENIED)]
        [RequireAdmin(ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        [RequireUser(BotProperties.ELEVATED_USERS, ErrorMessage = Constants.ERROR_DENIED, Group = "Perm")]
        public async Task ListProperties()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            foreach(var property in BotProperties.ALL_PROPERTIES)
            {
                if(BotProperties.RESTRICTED_PROPERTIES.Contains(property.Key))
                    continue;
                string value = _storage.GetProperty(property.Key);
                embedBuilder.AddField(
                    string.Format("{0} - {1}", property.Key, property.Value.Description),
                    string.Format("'{0}'", value));
            }
            await ReplyAsync("Current properties", false, embedBuilder.Build());
        }

    }
}
