using System;
using System.Reflection;

namespace Schmellow.DiscordServices.Pinger.Logging
{
    public sealed class NLogAdapter : ILogger
    {
        NLog.Logger _logger;

        public NLogAdapter(string loggerName)
        {
            // Init logger
            var assembly = Assembly.GetExecutingAssembly();
            NLog.LogManager.ThrowConfigExceptions = true;
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("logger.xml");
            if (NLog.LogManager.Configuration.AllTargets.Count == 0)
                throw new Exception("No logging targets - logger is not configured");
            if (!Environment.UserInteractive)
            {
                var consoleTarget = NLog.LogManager.Configuration.FindTargetByName("console");
                if (consoleTarget != null)
                    NLog.LogManager.Configuration.RemoveTarget("console");
            }
            _logger = NLog.LogManager.GetLogger(loggerName);
        }
        public void Critical(string message, params object[] args)
        {
            _logger.Fatal(message, args);
        }

        public void Critical(Exception ex, string message, params object[] args)
        {
            _logger.Fatal(ex, message, args);
        }

        public void Debug(string message, params object[] args)
        {
            _logger.Debug(message, args);
        }

        public void Debug(Exception ex, string message, params object[] args)
        {
            _logger.Debug(ex, message, args);
        }

        public void Error(string message, params object[] args)
        {
            _logger.Error(message, args);
        }

        public void Error(Exception ex, string message, params object[] args)
        {
            _logger.Error(ex, message, args);
        }

        public void Info(string message, params object[] args)
        {
            _logger.Info(message, args);
        }

        public void Info(Exception ex, string message, params object[] args)
        {
            _logger.Info(ex, message, args);
        }

        public void Trace(string message, params object[] args)
        {
            _logger.Trace(message, args);
        }

        public void Trace(Exception ex, string message, params object[] args)
        {
            _logger.Trace(ex, message, args);
        }

        public void Warn(string message, params object[] args)
        {
            _logger.Warn(message, args);
        }

        public void Warn(Exception ex, string message, params object[] args)
        {
            _logger.Warn(ex, message, args);
        }

        public void Dispose()
        {
            NLog.LogManager.Flush();
        }
    }
}
