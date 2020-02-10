using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Schmellow.DiscordServices.Pinger.Data;
using Schmellow.DiscordServices.Pinger.Models;
using Schmellow.DiscordServices.Pinger.Services;
using System;
using System.IO.Pipes;
using System.Linq;

namespace Schmellow.DiscordServices.Pinger
{
    public sealed class Program
    {
        private readonly static string PIPE_NAME = "Schmellow.DiscordServices.Pinger.";

        private static NLog.Logger _logger = null;
        private static string _instanceName = string.Empty;

        static Program()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {

            if(IsRunning())
            {
                e.Cancel = true;
                Stop();
            }
            else
            {
                e.Cancel = false;
            }
        }

        static void Main(string[] args)
        {
            try
            {
                if(args.Length == 0 || args[0] == "help")
                {
                    Console.WriteLine("Expecting command");
                    Console.WriteLine("Available commands:");
                    Console.WriteLine(" * run <instanceName> <arguments>");
                    Console.WriteLine("   --discord-token - discord auth token string [MANDATORY]");
                    Console.WriteLine("   --tracker-url - tracker service url [default=none]");
                    Console.WriteLine("   --tracker-token - tracker service auth token [default=none]");
                    Console.WriteLine("   --data-directory - db storage location [default=./]");
                    Console.WriteLine(" * stop <instanceName>");
                    return;
                }

                string command = args[0];

                BotProperties botProperties = ParseProperties(args.Skip(1).ToArray());
                _instanceName = botProperties.InstanceName;
                
                NLog.LayoutRenderers.LayoutRenderer.Register("instance", (logevent) => botProperties.InstanceName);
                NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("logger.config");
                _logger = NLog.LogManager.GetCurrentClassLogger();

                if (string.IsNullOrEmpty(_instanceName))
                    throw new ArgumentException("Instance name is not set");

                if (command == "run")
                {
                    Run(botProperties);
                }
                else if(command == "stop")
                {
                    Stop();
                }
                else
                {
                    throw new ArgumentException("Unknown command '" + command + "'");
                }
            }
            catch(Exception ex)
            {
                LogError(ex.ToString());
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        private static char[] _argSplitter = new char[] { '=' };
        private static BotProperties ParseProperties(string[] args)
        {
            BotProperties botProperties = new BotProperties();
            botProperties.InstanceName = args.FirstOrDefault(a => !a.StartsWith("--"));
            foreach (string arg in args.Where(a => a.StartsWith("--")))
            {
                var tokens = arg.Split(_argSplitter, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 2)
                    continue;
                string name = tokens[0].Trim('-');
                string value = tokens[1].Trim('\'').Trim('"');
                switch (name)
                {
                    case "discord-token":
                        botProperties.DiscordToken = value;
                        break;
                    case "tracker-url":
                        botProperties.TrackerUrl = value;
                        break;
                    case "tracker-token":
                        botProperties.TrackerToken = value;
                        break;
                    case "data-directory":
                        botProperties.DataDirectory = value;
                        break;
                    default:
                        LogInfo("Unknown parameter '{0}'", name);
                        break;
                }
            }
            return botProperties;
        }

        private static ServiceProvider ConfigureServices(BotProperties botProperties)
        {
            // configure services
            var services = new ServiceCollection();
            services.AddLogging(loggingBuilder =>
            {
                // configure Logging with NLog
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                loggingBuilder.AddNLog();
            });
            services.AddSingleton<IServiceProvider>(p => p);
            services.AddSingleton(botProperties);
            services.AddSingleton<LiteDBStorage>();
            services.AddSingleton<IGuildPropertyStorage>(p => p.GetRequiredService<LiteDBStorage>());
            services.AddSingleton<IEventStorage>(p => p.GetRequiredService<LiteDBStorage>());
            services.AddSingleton<DiscordSocketClient>();
            services.AddSingleton<IDiscordClient>(p => p.GetRequiredService<DiscordSocketClient>());
            services.AddSingleton<CommandService>(p =>
            {
                return new CommandService(new CommandServiceConfig()
                {
                    LogLevel = LogSeverity.Info,
                    CaseSensitiveCommands = false
                });
            });
            services.AddSingleton<MessagingService>();
            services.AddSingleton<TrackerService>();
            services.AddSingleton<SchedulingService>();
            services.AddSingleton<Bot>();
            return services.BuildServiceProvider();
        }

        private static void Run(BotProperties botProperties)
        {
            if (IsRunning())
                throw new Exception("Instance '" + _instanceName + "' is already running");

            LogInfo("Running instance '{0}'", _instanceName);

            if (string.IsNullOrEmpty(botProperties.DiscordToken))
                throw new ArgumentException("Discord token is not set");

            if (string.IsNullOrEmpty(botProperties.TrackerToken) ||
                string.IsNullOrEmpty(botProperties.TrackerUrl))
            {
                LogWarning("Tracker service properties are not set, PM commands will not be available");
            }

            using (var services = ConfigureServices(botProperties))
            {
                var bot = services.GetRequiredService<Bot>();
                bot.Run().GetAwaiter().GetResult();
                Block();
                bot.Stop().GetAwaiter().GetResult();
            }
        }

        public static void Stop()
        {
            if (!IsRunning())
                throw new Exception("Instance '" + _instanceName + "' is not running");
            LogInfo("Stopping instance '{0}'",  _instanceName);
            try
            {
                using (var client = new NamedPipeClientStream(".", PIPE_NAME + _instanceName, PipeDirection.Out))
                {
                    client.Connect(1000);
                    client.WriteByte(2);
                }
            }
            catch (TimeoutException)
            {
                // Suppress timeout exception
            }
        }

        private static void Block()
        {
            bool run = true;
            while (run)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PIPE_NAME + _instanceName, PipeDirection.InOut))
                    {
                        server.WaitForConnection();
                        switch (server.ReadByte())
                        {
                            case 1:
                                server.WriteByte(1);
                                break;
                            case 2:
                                run = false;
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex.ToString());
                    run = false;
                }
            }
        }

        private static bool IsRunning()
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PIPE_NAME + _instanceName, PipeDirection.InOut))
                {
                    client.Connect(1000);
                    client.WriteByte(1);
                    return client.ReadByte() == 1;
                }
            }
            catch (TimeoutException)
            {
                // Suppress timeout exception
            }
            return false;
        }

        private static void LogInfo(string message, params string[] args)
        {
            message = string.Format(message, args);
            if (_logger != null)
                _logger.Info(message);
            else
                Console.WriteLine(message);
        }

        private static void LogWarning(string message, params string[] args)
        {
            message = string.Format(message, args);
            if (_logger != null)
                _logger.Warn(message);
            else
                Console.WriteLine("WARNING: " + message);
        }

        private static void LogError(string message, params string[] args)
        {
            message = string.Format(message, args);
            if (_logger != null)
                _logger.Error(message);
            else
                Console.WriteLine("ERROR: " + message);
        }

    }
}
