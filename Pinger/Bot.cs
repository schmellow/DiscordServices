﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger
{
    public sealed class Bot
    {
        readonly ILogger _logger;
        readonly IStorage _storage;
        readonly IServiceProvider _serviceProvider;

        readonly DiscordSocketClient _client;
        readonly CommandService _commandService;

        public Bot(ILogger logger, IStorage storage)
        {
            _logger = logger;
            _storage = storage;

            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.MessageReceived += HandleMessage;

            _commandService = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false,
            });
            _commandService.Log += Log;
            _commandService.CommandExecuted += CommandExecuted;

            var services = new ServiceCollection();
            services.AddSingleton<ILogger>(_logger);
            services.AddSingleton<IStorage>(_storage);
            services.AddSingleton<DiscordSocketClient>(_client);
            _serviceProvider = services.BuildServiceProvider();
        }

        public async Task Run()
        {
            await _commandService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
            await _client.LoginAsync(
                TokenType.Bot,
                _storage.GetProperty(Constants.PROP_TOKEN).FirstOrDefault());
            await _client.StartAsync();
        }

        public async Task Stop()
        {
            _logger.Info("Shutting down");
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
            bool error = !string.IsNullOrEmpty(result?.ErrorReason);
            _logger.Info(
                "Command='{0}'; User='{1}'; Server='{2}'; Channel='{3}' | {4}",
                command.IsSpecified ? command.Value.Name : "",
                context.User == null ? "" : context.User.Username + "#" + context.User.Discriminator,
                context.Guild == null ? "" : context.Guild.Name,
                context.Channel == null ? "" : context.Channel.Name,
                error ? result?.ErrorReason : "SUCCESS");
            if(error)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}
