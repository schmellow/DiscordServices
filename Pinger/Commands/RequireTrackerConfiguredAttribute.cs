using Discord.Commands;
using Schmellow.DiscordServices.Pinger.Models;
using System;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    /// <summary>
    /// Checks that tracker service is configured
    /// </summary>
    public sealed class RequireTrackerConfiguredAttribute : PreconditionAttribute
    {
        private string _errorMessage;

        public RequireTrackerConfiguredAttribute()
        {
            
        }

        public override string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            var botProperties = services.GetService(typeof(BotProperties)) as BotProperties;
            if(string.IsNullOrEmpty(botProperties.TrackerUrl) ||
               string.IsNullOrEmpty(botProperties.TrackerToken))
            {
                return Task.FromResult(PreconditionResult.FromError(_errorMessage));
            }
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

    }

}
