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
        [RequireContext(ContextType.Guild)]
        [RequireChannel(BotProperties.CONTROL_CHANNELS)]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser(BotProperties.ELEVATED_USERS, Group = "Access")]
        public async Task ShowProperty([Remainder] string property)
        {
            if (BotProperties.ExistsAndUnrestricted(property))
            {
                string value = _storage.GetProperty(property);
                value = value == null ? "" : value;
                if(string.IsNullOrEmpty(value))
                {
                    await ReplyAsync(string.Format("Property '{0}' is not set", property));
                }
                else
                {
                    if (BotProperties.IsMulticolumn(property))
                        value = value.TrimEnd('|').Replace("|", ", ");
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
        [RequireContext(ContextType.Guild)]
        [RequireChannel(BotProperties.CONTROL_CHANNELS)]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser(BotProperties.ELEVATED_USERS, Group = "Access")]
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
            var names = new HashSet<string>();
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
                        {
                            names.Add(user.Username + "#" + user.Discriminator);
                        }
                        else
                        {
                            var role = Context.Guild.GetRole(id);
                            if (role != null)
                                names.Add(role.Name);
                        }
                    }
                }
            }
            // set users
            if(names.Count == 0)
            {
                await ReplyAsync("No users/roles found");
            }
            else
            {
                if(single)
                {
                    if(names.Count > 1)
                    {
                        await ReplyAsync(string.Format("Expected single user/role value, got {0}", names.Count));
                    }
                    else
                    {
                        await SetString(property, names.First());
                    }
                }
                else 
                {
                    await SetString(
                        property,
                        string.Join("|", names) + "|");
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
                        await ReplyAsync(string.Format("Expected single channel value, got {0}", channels.Count));
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
        [RequireContext(ContextType.Guild)]
        [RequireChannel(BotProperties.CONTROL_CHANNELS)]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser(BotProperties.ELEVATED_USERS, Group = "Access")]
        public async Task ListProperties()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            foreach(var property in BotProperties.ALL_PROPERTIES)
            {
                if(BotProperties.RESTRICTED_PROPERTIES.Contains(property.Key))
                    continue;
                string value = _storage.GetProperty(property.Key);
                value = value == null ? "" : value;
                if(BotProperties.IsMulticolumn(property.Key))
                    value = value.TrimEnd('|').Replace("|", ", ");
                embedBuilder.AddField(
                    string.Format("{0} - {1}", property.Key, property.Value.Description),
                    string.Format("'{0}'", value));
            }
            await ReplyAsync("Current properties", false, embedBuilder.Build());
        }

    }
}
