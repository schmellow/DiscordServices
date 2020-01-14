using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class ConfigurationModule : ModuleBase
    {
        ILogger _logger;
        Configuration _configuration;

        public ConfigurationModule(ILogger logger, Configuration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Command("set")]
        [Summary("Sets property")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        public async Task<RuntimeResult> SetProperty(string property, params string[] values)
        {
            try
            {
                // parse possible user/role/channel values that are passed as id string
                var idRegex = new Regex(@"<[\#\!\@\&]+(\d+)>");
                var parsedValues = new List<string>();
                foreach(string value in values)
                {
                    Match match = idRegex.Match(value);
                    ulong id;
                    if(match.Success && ulong.TryParse(match.Groups[1].Value, out id))
                    {
                        var user = await Context.Guild.GetUserAsync(id);
                        if (user != null)
                        {
                            parsedValues.Add(user.Username + "#" + user.Discriminator);
                            continue;
                        }
                        var role = Context.Guild.GetRole(id);
                        if (role != null)
                        {
                            parsedValues.Add(role.Name);
                            continue;
                        }
                        var channel = await Context.Guild.GetChannelAsync(id);
                        if(channel != null)
                        {
                            parsedValues.Add(channel.Name);
                            continue;
                        }
                    }
                    else
                    {
                        parsedValues.Add(value);
                    }
                }
                // set parsed values
                _configuration.SetProperty(Context.Guild.Id, property, parsedValues.ToArray());
                await ReplyAsync("Ok");
                return CommandResult.FromSuccess();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("show")]
        [Summary("Shows property info and value")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        public async Task<RuntimeResult> ShowProperty([Remainder] string property)
        {
            try
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.Title = _configuration.GetPropertyDescription(property);
                embedBuilder.Description = string.Format(
                    "'{0}'",
                    _configuration.GetPropertyAsString(Context.Guild.Id, property));
                await ReplyAsync(string.Empty, false, embedBuilder.Build());
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("properties")]
        [Summary("Lists all available properties")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        public async Task<RuntimeResult> ListProperties()
        {
            try
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.Title = "Available properties";
                foreach (var propertyName in _configuration.PropertyNames)
                {
                    string description = _configuration.GetPropertyDescription(propertyName);
                    string value = _configuration.GetPropertyAsString(Context.Guild.Id, propertyName);
                    embedBuilder.AddField(
                        string.Format("{0} - {1}", propertyName, description),
                        string.Format("'{0}'", value));
                }
                if (embedBuilder.Fields.Count == 0)
                {
                    await ReplyAsync("No properties available");
                }
                else
                {
                    await ReplyAsync("", false, embedBuilder.Build());
                }
                return CommandResult.FromSuccess();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

    }
}
