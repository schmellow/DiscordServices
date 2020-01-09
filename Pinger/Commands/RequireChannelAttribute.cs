using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    /// <summary>
    /// Checks that command is executed in channel whitelisted through specific ChannelProperty
    /// If ChannelProperty value is empty, then no restriction is applied
    /// </summary>
    public sealed class RequireChannelAttribute : PreconditionAttribute
    {
        string _errorMessage;
        string _channelProperty;

        public RequireChannelAttribute(string channelProperty)
        {
            _channelProperty = channelProperty;
        }

        public override string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            if (context.Channel is IGuildChannel gChannel)
            {
                var storage = (IStorage)services.GetService(typeof(IStorage));
                var propertyValue = storage.GetProperty(_channelProperty);

                if (string.IsNullOrEmpty(propertyValue))
                    return Task.FromResult(PreconditionResult.FromSuccess());

                if (propertyValue.Contains(gChannel.Name + "|"))
                    return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError(_errorMessage));
        }
    }
}
