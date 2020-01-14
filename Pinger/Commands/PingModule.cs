using Discord;
using Discord.Commands;
using Schmellow.DiscordServices.Pinger.Services;
using System;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class PingModule : ModuleBase
    {
        ILogger _logger;
        Configuration _configuration;
        MessagingService _messagingService;

        public PingModule(ILogger logger, Configuration configuration, MessagingService messagingService)
        {
            _logger = logger;
            _configuration = configuration;
            _messagingService = messagingService;
        }

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("Posts a ping into default ping channel")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> Ping([Remainder] string message)
        {
            try
            {
                var embedBuilder = new EmbedBuilder();
                embedBuilder.Description = message;
                embedBuilder.AddField("From " + Context.User.Username, "\u200b");
                await _messagingService.PingDefaultChannel(Context.Guild.Id, string.Empty, embedBuilder.Build());
                return CommandResult.FromSuccess();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }            
        }

        [Command("ping-channel", RunMode = RunMode.Async)]
        [Summary("Posts a ping into specified channel")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> PingChannel(IMessageChannel channel, [Remainder] string message)
        {
            try
            {
                var embedBuilder = new EmbedBuilder();
                embedBuilder.Description = message;
                embedBuilder.AddField("From " + Context.User.Username, "\u200b");
                await _messagingService.PingChannel(channel, string.Empty, embedBuilder.Build());
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
