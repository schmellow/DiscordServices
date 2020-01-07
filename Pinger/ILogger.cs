using System;

namespace Schmellow.DiscordServices.Pinger
{
    public interface ILogger : IDisposable
    {
        void Info(string message, params object[] args);
        void Info(Exception ex, string message, params object[] args);
        void Debug(string message, params object[] args);
        void Debug(Exception ex, string message, params object[] args);
        void Warn(string message, params object[] args);
        void Warn(Exception ex, string message, params object[] args);
        void Error(string message, params object[] args);
        void Error(Exception ex, string message, params object[] args);
        void Critical(string message, params object[] args);
        void Critical(Exception ex, string message, params object[] args);
        void Trace(string message, params object[] args);
        void Trace(Exception ex, string message, params object[] args);
    }
}
