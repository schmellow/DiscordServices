using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class HelpModule : ModuleBase
    {
        IServiceProvider _serviceProvider;
        CommandService _commandService;

        public HelpModule(IServiceProvider serviceProvider, CommandService commandService)
        {
            _serviceProvider = serviceProvider;
            _commandService = commandService;
        }

        [Command("help")]
        [Summary("Prints help message")]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> Help([Remainder] string module = "")
        {
            module = module.ToLowerInvariant();
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Title = "Available commands";
            string moduleName = "";
            foreach (CommandInfo command in _commandService.Commands)
            {
                if (command.Name == "help")
                    continue;
                string commandModule = command.Module.Name;
                if (!string.IsNullOrEmpty(module) && commandModule.ToLowerInvariant().StartsWith(module) == false)
                    continue;
                var check = await command.CheckPreconditionsAsync(Context, _serviceProvider);
                if (check.IsSuccess)
                {
                    if (commandModule != moduleName)
                    {
                        moduleName = commandModule;
                        embedBuilder.AddField(
                            "----------", 
                            Format.Bold(Format.Underline(moduleName.Replace("Module", "") + " commands:")));
                    }
                    string title = "!" + command.Name;
                    foreach (var parameter in command.Parameters)
                    {
                        if (parameter.IsMultiple)
                            title += string.Format(" [{0}]", parameter.Name);
                        else
                            title += string.Format(" <{0}>", parameter.Name);
                    }
                    var description = string.IsNullOrEmpty(command.Summary) ? "No description available" : command.Summary;
                    embedBuilder.AddField(title, description);
                }
            }
            if (embedBuilder.Fields.Count == 0)
            {
                await ReplyAsync("No commands available");
            }
            else
            {
                await ReplyAsync("", false, embedBuilder.Build());
            }
            return CommandResult.FromSuccess();
        }

    }
}
