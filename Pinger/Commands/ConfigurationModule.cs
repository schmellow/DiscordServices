using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Pinger.Data;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class ConfigurationModule : ModuleBase
    {
        ILogger<ConfigurationModule> _logger;
        IGuildPropertyStorage _guildPropertyStorage;

        public ConfigurationModule(
            ILogger<ConfigurationModule> logger,
            IGuildPropertyStorage guildPropertyStorage)
        {
            _logger = logger;
            _guildPropertyStorage = guildPropertyStorage;
        }

        [Command("set")]
        [Summary("Sets property")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Elevated)]
        public async Task<RuntimeResult> SetProperty(string property, params string[] values)
        {
            try
            {
                var guildProperties = _guildPropertyStorage.EnsureGuildProperties(Context.Guild.Id);
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
                guildProperties.SetProperty(property, string.Join(";", parsedValues));
                _guildPropertyStorage.UpdateGuildProperties(guildProperties);
                await ReplyAsync("Ok");
                return CommandResult.FromSuccess();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("show")]
        [Summary("Shows property info and value")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Elevated)]
        public async Task<RuntimeResult> ShowProperty([Remainder] string property)
        {
            try
            {
                var guildProperties = _guildPropertyStorage.EnsureGuildProperties(Context.Guild.Id);
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.Title = guildProperties.GetPropertyDescription(property);
                embedBuilder.Description = string.Format(
                    "'{0}'",
                    guildProperties.GetProperty(property));
                await ReplyAsync(string.Empty, false, embedBuilder.Build());
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("properties")]
        [Summary("Lists all available properties")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Elevated)]
        public async Task<RuntimeResult> ListProperties()
        {
            try
            {
                var guildProperties = _guildPropertyStorage.EnsureGuildProperties(Context.Guild.Id);
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.Title = "Available properties";
                foreach(var name in guildProperties.GetPropertyNames())
                {
                    string description = guildProperties.GetPropertyDescription(name);
                    string value = guildProperties.GetProperty(name);
                    embedBuilder.AddField(
                        string.Format("{0} - {1}", name, description),
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
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

    }
}
