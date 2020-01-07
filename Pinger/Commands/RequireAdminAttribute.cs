using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    /// <summary>
    /// Checks that user is Guild Admin
    /// </summary>
    public sealed class RequireAdminAttribute : PreconditionAttribute
    {
        string _errorMessage;

        public override string ErrorMessage { get => _errorMessage ; set => _errorMessage = value; }

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, 
            CommandInfo command, 
            IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                if (gUser.GuildPermissions.Administrator)
                    return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError(_errorMessage));
        }
    }
}
