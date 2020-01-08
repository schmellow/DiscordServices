using System;
using System.Collections.Generic;
using System.Text;

namespace Schmellow.DiscordServices.Pinger.Logging
{
    public sealed class FallbackConsoleLogger : ILogger
    {
        public void Critical(string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("CRIT", message, args));
        }

        public void Critical(Exception ex, string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("CRIT", ex, message, args));
        }

        public void Debug(string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("DEBUG", message, args));
        }

        public void Debug(Exception ex, string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("DEBUG", ex, message, args));
        }

        public void Error(string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("ERROR", message, args));
        }

        public void Error(Exception ex, string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("ERROR", ex, message, args));
        }

        public void Info(string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("INFO", message, args));
        }

        public void Info(Exception ex, string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("INFO", ex, message, args));
        }

        public void Trace(string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("TRACE", message, args));
        }

        public void Trace(Exception ex, string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("TRACE", ex, message, args));
        }

        public void Warn(string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("WARN", message, args));
        }

        public void Warn(Exception ex, string message, params object[] args)
        {
            Console.WriteLine(FormatMessage("WARN", ex, message, args));
        }

        public void Dispose()
        {
            // No-op
        }

        string FormatMessage(string level, string message, params object[] args)
        {
            return string.Format(
                "{0}| {1}| {2}",
                DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                level,
                string.Format(message, args));
        }

        string FormatMessage(string level, Exception ex, string message, params object[] args)
        {
            return string.Format(
                "{0}| {1}| {2} {3}",
                DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                level,
                string.Format(message, args),
                ex);
        }
    }
}
