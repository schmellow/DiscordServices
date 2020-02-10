using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Schmellow.DiscordServices.Pinger.Data;
using System;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    /// <summary>
    /// Checks that command is executed in channel whitelisted through specific ChannelProperty
    /// If ChannelProperty value is empty, then no restriction is applied
    /// </summary>
    public sealed class RequireControlChannelAttribute : PreconditionAttribute
    {
        string _errorMessage;

        public RequireControlChannelAttribute()
        {

        }

        public override string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            if(context.User is SocketGuildUser gUser && gUser.GuildPermissions.Administrator)
                return Task.FromResult(PreconditionResult.FromSuccess());

            if (context.Channel is IGuildChannel gChannel)
            {
                var guildPropertyStorage = services.GetService(typeof(IGuildPropertyStorage)) as IGuildPropertyStorage;
                var guildProperties = guildPropertyStorage.EnsureGuildProperties(context.Guild.Id);
             
                if(string.IsNullOrEmpty(guildProperties.ControlChannels) ||
                    guildProperties.ControlChannels.Contains(gChannel.Name))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
            return Task.FromResult(PreconditionResult.FromError(_errorMessage));
        }
    }
}
