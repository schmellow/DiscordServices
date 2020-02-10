using Discord.Commands;
using Discord.WebSocket;
using Schmellow.DiscordServices.Pinger.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public enum PermissionLevel
    {
        None = 0,
        Pings,
        Elevated,
        Admin
    }
    /// <summary>
    /// Checks that command is executed in by user whitelisted through specific UserProperty
    /// </summary>
    public sealed class RequirePermissionLevelAttribute : PreconditionAttribute
    {
        private readonly int _targetLevel;
        
        private string _errorMessage;

        public RequirePermissionLevelAttribute(PermissionLevel targetLevel)
        {
            _targetLevel = (int)targetLevel;
        }

        public override string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            int currentLevel = 0;
            if (context.User is SocketGuildUser gUser)
            {
                if(gUser.GuildPermissions.Administrator)
                {
                    currentLevel = (int)PermissionLevel.Admin;
                }
                else
                {
                    var userValue = gUser.Username + "#" + gUser.Discriminator;
                    var guildPropertyStorage = services.GetService(typeof(IGuildPropertyStorage)) as IGuildPropertyStorage;
                    var guildProperties = guildPropertyStorage.EnsureGuildProperties(context.Guild.Id);
                    if (guildProperties.ElevatedUsers.Contains(userValue) ||
                        gUser.Roles.Any(r => guildProperties.ElevatedUsers.Contains(r.Name)))
                    {
                        currentLevel = (int)PermissionLevel.Elevated;
                    }
                    else if(guildProperties.PingUsers.Contains(userValue) ||
                        gUser.Roles.Any(r => guildProperties.PingUsers.Contains(r.Name)))
                    {
                        currentLevel = (int)PermissionLevel.Pings;
                    }
                }
            }
            if (currentLevel >= _targetLevel)
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError(_errorMessage));
        }

    }

}
