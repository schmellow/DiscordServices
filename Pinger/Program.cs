using Schmellow.DiscordServices.Pinger.Logging;
using Schmellow.DiscordServices.Pinger.Storage;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Schmellow.DiscordServices.Pinger
{
    public sealed class Program
    {
        static EventWaitHandle _exitEventHandle;

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
            if (_exitEventHandle != null)
            {
                e.Cancel = true;
                _exitEventHandle.Set();
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
                string instanceName = args[0].Trim();
                string command = args[1].Trim();
                List<string> commandArgs = new List<string>();
                for (int i = 2; i < args.Length; i++)
                    commandArgs.Add(args[i].Trim());
                if (!EventWaitHandle.TryOpenExisting(Constants.EVENT_NAME_EXIT + instanceName, out _exitEventHandle))
                    _exitEventHandle = null;
                MainInternal(instanceName, command, commandArgs.ToArray());
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void MainInternal(string instanceName, string command, string[] commandArgs)
        {
            using (var logger = new NLogAdapter("bot-" + instanceName))
            {
                try
                {
                    logger.Info("Instance '{0}'", instanceName);
                    logger.Info("Running in '{0}'", System.IO.Directory.GetCurrentDirectory());
                    switch (command)
                    {
                        case "run":
                            RunBot(logger, instanceName, commandArgs);
                            break;
                        case "stop":
                            StopBot(logger, instanceName, commandArgs);
                            break;
                        case "set-property":
                            SetBotProperty(logger, instanceName, commandArgs);
                            break;
                        case "show-property":
                            ShowBotProperty(logger, instanceName, commandArgs);
                            break;
                        case "list-properties":
                            ListBotProperties(logger, instanceName, commandArgs);
                            break;
                        default:
                            logger.Error("Unknown command '{0}'", command);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, ex.Message);
                }
            }
        }

        static void RunBot(ILogger logger, string instanceName, string[] commandArgs)
        {
            if(_exitEventHandle != null)
            {
                logger.Error("RUN: instance '{0}' already running", instanceName);
                return;
            }
            logger.Info("RUN: Running instance '{0}'", instanceName);
            using(var storage = new LiteDbStorage(instanceName))
            {
                var bot = new Bot(logger, storage);
                bot.Run().GetAwaiter().GetResult();
                _exitEventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Constants.EVENT_NAME_EXIT + instanceName);
                _exitEventHandle.WaitOne();
                bot.Stop().GetAwaiter().GetResult();
            }
        }

        static void StopBot(ILogger logger, string instanceName, string[] commandArgs)
        {
            if(_exitEventHandle == null)
            {
                logger.Error("STOP: No instance '{0}' detected to stop", instanceName);
                return;
            }
            logger.Info("STOP: Instance '{0}' detected running, stopping", instanceName);
            _exitEventHandle.Set();
        }

        static void SetBotProperty(ILogger logger, string instanceName, string[] commandArgs)
        {
            if(commandArgs.Length < 2)
            {
                logger.Error("SET-PROPERTY: Expecting command arguments - <property> [values]");
                return;
            }
            using(var storage = new LiteDbStorage(instanceName))
            {
                logger.Info("SET-PROPERTY[{0}]", commandArgs[0]);
                var values = new HashSet<string>();
                for (int i = 1; i < commandArgs.Length; i++)
                    values.Add(commandArgs[i]);
                storage.SetProperty(commandArgs[0], values);
            }
        }

        static void ShowBotProperty(ILogger logger, string instanceName, string[] commandArgs)
        {
            if (commandArgs.Length < 1)
            {
                logger.Error("SHOW-PROPERTY: Expecting command arguments - <property>");
                return;
            }
            using (var storage = new LiteDbStorage(instanceName))
            {
                logger.Info("SHOW-PROPERTY[{0}]", commandArgs[0]);
                var valueString = string.Join(",", storage.GetProperty(commandArgs[0]));
                Console.WriteLine("{0}='{1}'", commandArgs[0], valueString);
            }
        }

        static void ListBotProperties(ILogger logger, string instanceName, string[] commandArgs)
        {
            using(var storage = new LiteDbStorage(instanceName))
            {
                logger.Info("LIST-PROPERTIES");
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
