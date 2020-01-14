using Discord.Commands;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class CommandResult : RuntimeResult
    {
        public CommandResult() : base (null, null) { }

        public CommandResult(CommandError? error, string reason) : base(error, reason)
        {

        }

        public static CommandResult FromError(string reason) => new CommandResult(CommandError.Unsuccessful, reason);
        public static CommandResult FromSuccess() => new CommandResult();
    }
}
