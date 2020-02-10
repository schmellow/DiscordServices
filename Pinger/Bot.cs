using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Pinger.Models;
using Schmellow.DiscordServices.Pinger.Services;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger
{
    public sealed class Bot
    {
        private readonly ILogger<Bot> _logger;
        private readonly BotProperties _botProperties;
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly SchedulingService _schedulingService;

        private bool _stopCalled = false;

        public Bot(
            ILogger<Bot> logger,
            BotProperties botProperties,
            IServiceProvider serviceProvider,
            DiscordSocketClient client,
            CommandService commandService,
            SchedulingService schedulingService)
        {
            _logger = logger;
            _botProperties = botProperties;
            _serviceProvider = serviceProvider;
            _client = client;
            _commandService = commandService;
            _schedulingService = schedulingService;

            _client.Log += Log;
            _client.MessageReceived += HandleMessage;
            _client.Ready += ClientReady;
            _client.Disconnected += HandleDisconnect;

            _commandService.Log += Log;
            _commandService.CommandExecuted += CommandExecuted;
        }

        public async Task Run()
        {
            await _commandService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
            await _client.LoginAsync(TokenType.Bot, _botProperties.DiscordToken);
            await _client.StartAsync();
        }

        private async Task ClientReady()
        {
            var schedulingService = _serviceProvider.GetRequiredService<SchedulingService>();
            await Task.Run(() => schedulingService.Run());
        }

        public async Task Stop()
        {
            _logger.LogInformation("Shutting down");
            _stopCalled = true;
            await Task.Run(() => _schedulingService.Stop());
            await _client.StopAsync();
        }

        Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Info:
                    _logger.LogInformation(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Error:
                    _logger.LogError(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Critical:
                    _logger.LogCritical(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Debug:
                    _logger.LogDebug(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace(msg.Exception, msg.Message);
                    break;
                default:
                    _logger.LogInformation(msg.Exception, msg.Message);
                    break;
            }
            return Task.CompletedTask;
        }

        async Task HandleMessage(SocketMessage messageParam)
        {
            // Bail out on system messages
            var message = messageParam as SocketUserMessage;
            if (message == null)
                return;

            // Bail out on messages from other bots and itself
            if (message.Author.IsBot ||
                message.Author.Id == _client.CurrentUser.Id)
            {
                return;
            }

            int argPos = 0;
            // Bail out if message is not command (! prefix) and not addressed to the bot
            if (!message.HasCharPrefix('!', ref argPos) &&
                !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                return;
            }

            var commandService = _serviceProvider.GetRequiredService<CommandService>();
            await commandService.ExecuteAsync(
                new SocketCommandContext(_client, message),
                argPos,
                _serviceProvider);
        }

        async Task CommandExecuted(
            Optional<CommandInfo> command,
            ICommandContext context,
            IResult result)
        {
            // Exceptions are already logged automatically
            bool error = result?.IsSuccess != true;
            string outcome;
            if (error)
            {
                if (result?.Error == CommandError.UnmetPrecondition)
                    outcome = "Denied";
                else
                    outcome = "Error: " + result?.ErrorReason;
            }
            else
            {
                outcome = "OK";
            }
            _logger.LogInformation(
                "Command='{0}'; User='{1}'; Server='{2}'; Channel='{3}' | {4}",
                command.IsSpecified ? command.Value.Name : "",
                context.User == null ? "" : context.User.Username + "#" + context.User.Discriminator,
                context.Guild == null ? "" : context.Guild.Name,
                context.Channel == null ? "" : context.Channel.Name,
                outcome);
            if (error)
                await context.Channel.SendMessageAsync(outcome);
        }

        private Task HandleDisconnect(Exception arg)
        {
            if(!_stopCalled) // ignore manual stop
            {
                _logger.LogCritical(arg, arg.Message);
                Program.Stop();
            }
            return Task.CompletedTask;
        }

    }
}
