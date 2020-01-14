using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Schmellow.DiscordServices.Pinger.Services;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger
{
    public sealed class Bot
    {
        readonly ILogger _logger;
        readonly Configuration _configuration;
        readonly IServiceProvider _serviceProvider;

        readonly DiscordSocketClient _client;
        readonly CommandService _commandService;
        readonly MessagingService _messagingService;
        readonly SchedulingService _schedulingService;

        public Bot(ILogger logger, Configuration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.MessageReceived += HandleMessage;
            _client.Ready += ClientReady;

            _commandService = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false,
            });
            _commandService.Log += Log;
            _commandService.CommandExecuted += CommandExecuted;
            
            _messagingService = new MessagingService(_logger, _configuration, _client);
            _schedulingService = new SchedulingService(_logger, _configuration);
            _schedulingService.PingEvent += _messagingService.PingEvent;

            var services = new ServiceCollection();
            services.AddSingleton<ILogger>(_logger);
            services.AddSingleton<Configuration>(_configuration);
            services.AddSingleton<DiscordSocketClient>(_client);
            services.AddSingleton<MessagingService>(_messagingService);
            services.AddSingleton<SchedulingService>(_schedulingService);
            _serviceProvider = services.BuildServiceProvider();
        }

        public async Task Run()
        {
            await _commandService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
            await _client.LoginAsync(TokenType.Bot, _configuration.Token);
            await _client.StartAsync();
        }

        private async Task ClientReady()
        {
            await Task.Run(() => _schedulingService.Run());
        }

        public async Task Stop()
        {
            _logger.Info("Shutting down");
            if (_schedulingService != null)
                await Task.Run(() => _schedulingService.Stop());
            if (_client != null)
                await _client.StopAsync();
        }

        Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Info:
                    _logger.Info(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Error:
                    _logger.Error(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.Warn(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Critical:
                    _logger.Critical(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Debug:
                    _logger.Debug(msg.Exception, msg.Message);
                    break;
                case LogSeverity.Verbose:
                    _logger.Trace(msg.Exception, msg.Message);
                    break;
                default:
                    _logger.Info(msg.Exception, msg.Message);
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
            if (message.Author.IsBot || message.Author.Id == _client.CurrentUser.Id)
                return;

            int argPos = 0;
            // Bail out if message is not command (! prefix) and not addressed to the bot
            if (!message.HasCharPrefix('!', ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            await _commandService.ExecuteAsync(
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
            _logger.Info(
                "Command='{0}'; User='{1}'; Server='{2}'; Channel='{3}' | {4}",
                command.IsSpecified ? command.Value.Name : "",
                context.User == null ? "" : context.User.Username + "#" + context.User.Discriminator,
                context.Guild == null ? "" : context.Guild.Name,
                context.Channel == null ? "" : context.Channel.Name,
                outcome);
            if(error)
                await context.Channel.SendMessageAsync(outcome);
        }

    }
}
