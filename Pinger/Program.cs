using Schmellow.DiscordServices.Pinger.Logging;
using Schmellow.DiscordServices.Pinger.Storage;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Schmellow.DiscordServices.Pinger
{
    public sealed class Program
    {
        static ILogger _logger;
        static string _instanceName;

        static Program()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            var workDir = Environment.GetEnvironmentVariable(Constants.ENV_WORKDIR);
            if (!string.IsNullOrEmpty(workDir))
            {
                workDir = workDir.Trim();
                System.IO.Directory.SetCurrentDirectory(workDir);
            }
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (InstanceHelper.IsRunning(_logger, _instanceName))
            {
                e.Cancel = true;
                InstanceHelper.Stop(_logger, _instanceName); ;
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
                    Console.WriteLine(" - set-property <property> <value>");
                    Console.WriteLine(" - show-property <property>");
                    Console.WriteLine(" - list-properties");
                    Console.WriteLine("Use '{0}' variable to override working directory", Constants.ENV_WORKDIR);
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
                    _logger = new NLogAdapter("bot-" + _instanceName);
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
                        RunBot(commandArgs);
                        break;
                    case "stop":
                        StopBot(commandArgs);
                        break;
                    case "set-property":
                        SetBotProperty(commandArgs);
                        break;
                    case "show-property":
                        ShowBotProperty(commandArgs);
                        break;
                    case "list-properties":
                        ListBotProperties(commandArgs);
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
                if (_logger != null)
                    _logger.Dispose();
            }
        }

        static void RunBot(string[] commandArgs)
        {
            if(InstanceHelper.IsRunning(_logger, _instanceName))
            {
                _logger.Error("RUN: instance '{0}' already running", _instanceName);
                return;
            }
            _logger.Info("RUN: Running instance '{0}'", _instanceName);
            using(var storage = new LiteDbStorage(_instanceName))
            {
                var bot = new Bot(_logger, storage);
                bot.Run().GetAwaiter().GetResult();
                InstanceHelper.Run(_logger, _instanceName);
                bot.Stop().GetAwaiter().GetResult();
            }
        }

        static void StopBot(string[] commandArgs)
        {
            if(!InstanceHelper.IsRunning(_logger, _instanceName))
            {
                _logger.Error("STOP: No instance '{0}' detected to stop", _instanceName);
                return;
            }
            _logger.Info("STOP: Instance '{0}' detected running, stopping", _instanceName);
            InstanceHelper.Stop(_logger, _instanceName);
        }

        static void SetBotProperty(string[] commandArgs)
        {
            if(commandArgs.Length < 1)
            {
                _logger.Error("SET-PROPERTY: Expecting command arguments - <property> [values]");
                return;
            }
            using(var storage = new LiteDbStorage(_instanceName))
            {
                _logger.Info("SET-PROPERTY[{0}]", commandArgs[0]);
                var values = new HashSet<string>();
                for (int i = 1; i < commandArgs.Length; i++)
                {
                    if(!string.IsNullOrEmpty(commandArgs[i]))
                        values.Add(commandArgs[i]);
                }
                storage.SetProperty(commandArgs[0], values);
            }
        }

        static void ShowBotProperty(string[] commandArgs)
        {
            if (commandArgs.Length < 1)
            {
                _logger.Error("SHOW-PROPERTY: Expecting command arguments - <property>");
                return;
            }
            using (var storage = new LiteDbStorage(_instanceName))
            {
                _logger.Info("SHOW-PROPERTY[{0}]", commandArgs[0]);
                var valueString = string.Join(",", storage.GetProperty(commandArgs[0]));
                Console.WriteLine("{0}='{1}'", commandArgs[0], valueString);
            }
        }

        static void ListBotProperties(string[] commandArgs)
        {
            using(var storage = new LiteDbStorage(_instanceName))
            {
                _logger.Info("LIST-PROPERTIES");
                var propertyNames = storage.GetPropertyNames();
                foreach(string propertyName in propertyNames)
                {
                    var value = string.Join(",", storage.GetProperty(propertyName));
                    Console.WriteLine("{0}='{1}'", propertyName, value);
                }
            }
        }

    }
}
