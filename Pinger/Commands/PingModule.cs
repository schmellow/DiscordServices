using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Pinger.Models;
using Schmellow.DiscordServices.Pinger.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class PingModule : ModuleBase
    {
        ILogger<PingModule> _logger;
        MessagingService _messagingService;
        TrackerService _trackerService;

        public PingModule(
            ILogger<PingModule> logger, 
            MessagingService messagingService, 
            TrackerService trackerService)
        {
            _logger = logger;
            _messagingService = messagingService;
            _trackerService = trackerService;
        }

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("Posts a ping into default ping channel")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> PingDefault([Remainder] string message)
        {
            try
            {
                await _messagingService.PingDefaultChannel(
                    Context.Guild.Id,
                    FormatMessage(message));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("ping-channel", RunMode = RunMode.Async)]
        [Summary("Posts a ping into specified channel")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> PingChannel(IMessageChannel channel, [Remainder] string message)
        {
            try
            {
                await _messagingService.PingChannel(
                    channel,
                    FormatMessage(message));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("pm", RunMode = RunMode.Async)]
        [Summary("Pings default ping channel members privately")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        [RequireTrackerConfigured(ErrorMessage = "Command is not available ATM")]
        public async Task<RuntimeResult> PMDefault([Remainder] string message)
        {
            try
            {
                if(_messagingService.MassDMInProgress)
                    throw new Exception("Mass DM operation is in progress, please wait for it to finish");
                var urls = await _trackerService.GetTrackerUrlsDefault(
                    Context.Guild.Id,
                    Context.User.Id,
                    message);
                var pms = urls.Select(kv => new PrivateMessage(kv.Key, FormatMessage(kv.Value))).ToArray();
                await _messagingService.MassDM(Context.Channel, pms);
                return CommandResult.FromSuccess();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("pm-channel", RunMode = RunMode.Async)]
        [Summary("Pings channel members privately")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        [RequireTrackerConfigured(ErrorMessage = "Command is not available ATM")]
        public async Task<RuntimeResult> PMChannel(IMessageChannel channel, [Remainder] string message)
        {
            try
            {
                if (_messagingService.MassDMInProgress)
                    throw new Exception("Mass DM operation is in progress, please wait for it to finish");
                var urls = await _trackerService.GetTrackerUrls(
                    Context.User,
                    channel,
                    message);
                var pms = urls.Select(kv => new PrivateMessage(kv.Key, FormatMessage(kv.Value))).ToArray();
                await _messagingService.MassDM(Context.Channel, pms);
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        private string FormatMessage(string message)
        {
            return string.Format("**From {0}:**\n{1}", Context.User.Username, message);
        }
    }
}
