using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class HelpModule : ModuleBase
    {
        IStorage _storage;
        IServiceProvider _serviceProvider;
        CommandService _commandService;

        public HelpModule(IStorage storage, IServiceProvider serviceProvider, CommandService commandService)
        {
            _storage = storage;
            _serviceProvider = serviceProvider;
            _commandService = commandService;
        }

        [Command("help")]
        [Summary("Prints help message")]
        [RequireContext(ContextType.Guild, ErrorMessage = Constants.ERROR_DENIED)]
        public async Task Help()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            foreach (CommandInfo command in _commandService.Commands)
            {
                if (command.Name == "help")
                    continue;
                var check = await command.CheckPreconditionsAsync(Context, _serviceProvider);
                if (check.IsSuccess)
                {
                    string title = command.Name;
                    foreach (var parameter in command.Parameters)
                    {
                        if (parameter.IsMultiple)
                            title += string.Format(" [{0}]", parameter.Name);
                        else
                            title += string.Format(" <{0}>", parameter.Name);
                    }
                    var description = string.IsNullOrEmpty(command.Summary) ? "No info available" : command.Summary;
                    embedBuilder.AddField(title, description);
                }
            }
            if (embedBuilder.Fields.Count == 0)
            {
                await ReplyAsync("No commands available");
            }
            else
            {
                await ReplyAsync(string.Empty, false, embedBuilder.Build());
            }
        }
    }
}
