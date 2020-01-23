using Schmellow.DiscordServices.Pinger.Logging;
using Schmellow.DiscordServices.Pinger.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Schmellow.DiscordServices.Pinger
{
    public sealed class Program
    {
        public const string ENV_WORKDIR = "PINGER_WORKDIR";

        static ILogger _logger;
        static string _instanceName;

        static Program()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            var workDir = Environment.GetEnvironmentVariable(ENV_WORKDIR);
            if (!string.IsNullOrEmpty(workDir))
            {
                workDir = workDir.Trim();
                System.IO.Directory.SetCurrentDirectory(workDir);
            }
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (InstanceManager.IsRunning(_logger, _instanceName))
            {
                e.Cancel = true;
                InstanceManager.Stop(_logger, _instanceName); ;
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
                if(args.Length > 0 && args[0] == "help")
                {
                    Console.WriteLine("Available commands:");
                    Console.WriteLine(" - run");
                    Console.WriteLine(" - stop");
                    Console.WriteLine(" - set-token <token>");
                    Console.WriteLine("Use '{0}' variable to override working directory", ENV_WORKDIR);
                    return;
                }
                else if(args.Length < 2)
                {
                    Console.WriteLine("Expecting arguments: <instanceName> <command> or help");
                    return;
                }
                // Begin init
                _instanceName = args[0].Trim();
                try
                {
                    _logger = new NLogAdapter(_instanceName);
                }
                catch (Exception ex)
                {
                    _logger = new FallbackConsoleLogger();
                    _logger.Error(ex, ex.Message);
                }
                _logger.Info("Instance '{0}'", _instanceName);
                _logger.Info("Running in '{0}'", System.IO.Directory.GetCurrentDirectory());
                // Parse and execute command
                string command = args[1].Trim();
                List<string> commandArgsList = new List<string>();
                for (int i = 2; i < args.Length; i++)
                    commandArgsList.Add(args[i].Trim());
                string[] commandArgs = commandArgsList.ToArray();
                switch (command)
                {
                    case "run":
                        Run(commandArgs);
                        break;
                    case "stop":
                        Stop(commandArgs);
                        break;
                    case "set-token":
                        SetToken(commandArgs);
                        break;
                    default:
                        _logger.Error("Unknown command '{0}'", command);
                        break;
                }
            }
            catch(Exception ex)
            {
                if (_logger != null)
                    _logger.Error(ex, ex.Message);
                else
                    Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (_logger != null && _logger is IDisposable)
                    ((IDisposable)_logger).Dispose();
            }
        }

        static void Run(string[] commandArgs)
        {
            if(InstanceManager.IsRunning(_logger, _instanceName))
            {
                _logger.Error("Instance '{0}' already running", _instanceName);
                return;
            }
            _logger.Info("Running instance '{0}'", _instanceName);
            var configuration = Configuration.Load(_instanceName);
            var bot = new Bot(_logger, configuration);
            bot.Run().GetAwaiter().GetResult();
            InstanceManager.Run(_logger, _instanceName);
            bot.Stop().GetAwaiter().GetResult();
        }

        static void Stop(string[] commandArgs)
        {
            if(!InstanceManager.IsRunning(_logger, _instanceName))
            {
                _logger.Error("No instance '{0}' detected to stop", _instanceName);
                return;
            }
            _logger.Info("Instance '{0}' detected running, stopping", _instanceName);
            InstanceManager.Stop(_logger, _instanceName);
        }

        static void SetToken(string[] commandArgs)
        {
            if(commandArgs.Length < 1)
            {
                _logger.Error("Expecting token string");
                return;
            }
            _logger.Info("Setting token for instance '{0}'", _instanceName);
            string token = commandArgs[0];
            var configuration = Configuration.Load(_instanceName);
            configuration.Token = token;
            configuration.Save();
        }

        public static void Stop()
        {
            InstanceManager.Stop(_logger, _instanceName);
        }

    }
}
