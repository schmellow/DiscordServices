using Schmellow.DiscordServices.Pinger.Logging;
using Schmellow.DiscordServices.Pinger.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

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
                        Run(commandArgs);
                        break;
                    case "stop":
                        Stop(commandArgs);
                        break;
                    case "set-property":
                        SetProperty(commandArgs);
                        break;
                    case "show-property":
                        ShowProperty(commandArgs);
                        break;
                    case "list-properties":
                        ListProperties(commandArgs);
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

        static void Run(string[] commandArgs)
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

        static void Stop(string[] commandArgs)
        {
            if(!InstanceHelper.IsRunning(_logger, _instanceName))
            {
                _logger.Error("STOP: No instance '{0}' detected to stop", _instanceName);
                return;
            }
            _logger.Info("STOP: Instance '{0}' detected running, stopping", _instanceName);
            InstanceHelper.Stop(_logger, _instanceName);
        }

        static void SetProperty(string[] commandArgs)
        {
            if(commandArgs.Length < 1)
            {
                _logger.Error("SET-PROPERTY: Expecting command arguments - <property> <value>");
                return;
            }
            _logger.Info("SET-PROPERTY[{0}]", commandArgs[0]);
            string property = commandArgs[0];
            if (!BotProperties.ALL_PROPERTIES.ContainsKey(property))
            {
                _logger.Error("Property {0} does not exist", property);
                return;
            }
            string value = "";
            if (commandArgs.Length > 1)
                value = string.Join(" ", commandArgs.Skip(1));
            var type = BotProperties.ALL_PROPERTIES[property].Type;
            if(!string.IsNullOrEmpty(value) && BotProperties.IsMulticolumn(property))
            {
                value = value.Replace(",", " ").Replace(";", " ").Replace("|", " ");
                string[] tokens = value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                value = string.Join("|", tokens) + "|";
            }
            using (var storage = new LiteDbStorage(_instanceName))
            {
                storage.SetProperty(property, value);
            }
        }

        static void ShowProperty(string[] commandArgs)
        {
            if (commandArgs.Length < 1)
            {
                _logger.Error("SHOW-PROPERTY: Expecting command arguments - <property>");
                return;
            }
            _logger.Info("SHOW-PROPERTY[{0}]", commandArgs[0]);
            string property = commandArgs[0];
            using (var storage = new LiteDbStorage(_instanceName))
            {
                string value = storage.GetProperty(property);
                Console.WriteLine("{0}='{1}'", property, value);
            }
        }

        static void ListProperties(string[] commandArgs)
        {
            using(var storage = new LiteDbStorage(_instanceName))
            {
                _logger.Info("LIST-PROPERTIES");
                foreach(string property in BotProperties.ALL_PROPERTIES.Keys)
                {
                    Console.WriteLine("{0}='{1}'", property, storage.GetProperty(property));
                }
            }
        }

    }
}
