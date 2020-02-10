using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Pinger.Data;
using Schmellow.DiscordServices.Pinger.Models;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public sealed partial class SchedulingService : IDisposable
    {
        private static readonly object _sync = new object();

        private readonly ILogger<SchedulingService> _logger;
        private readonly IGuildPropertyStorage _guildPropertyStorage;
        private readonly IEventStorage _eventStorage;
        private readonly MessagingService _messagingService;

        private Timer _timer;
        private ScheduledPing _nextPing;

        public delegate Task PingDelegate(ulong guildId, string channelName, string message);
        public PingDelegate PingMethod { get; private set; }
        public bool IsRunning { get; private set; }

        public SchedulingService(
            ILogger<SchedulingService> logger, 
            IGuildPropertyStorage guildPropertyStorage,
            IEventStorage eventStorage,
            MessagingService messagingService)
        {
            _logger = logger;
            _guildPropertyStorage = guildPropertyStorage;
            _eventStorage = eventStorage;
            _messagingService = messagingService;
        }

        public void Run()
        {
            lock (_sync)
            {
                _logger.LogInformation("Running scheduler");
                FullUpdate();
                IsRunning = true;
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                _logger.LogInformation("Stopping scheduler");
                RecycleTimer(true);
                IsRunning = false;
            }
        }

        public void Dispose()
        {
            if (_eventStorage != null && _eventStorage is IDisposable ds)
                ds.Dispose();
        }

        private async Task PingEventAsync(ulong guildId, DateTime pingDate, ScheduledEvent se)
        {
            string message = string.Format(
                "**{0}\nFrom {1}:**\n{2}",
                se.ETA,
                se.User,
                se.Message);

            var properties = _guildPropertyStorage.EnsureGuildProperties(guildId);
            string channel;
            if (se.TargetDate > pingDate)
            {
                _logger.LogInformation("Pinging reminder for event {0}/[{1}]", guildId, se.Id);
                channel = properties.RemindChannel;
                if(string.IsNullOrEmpty(channel))
                {
                    _logger.LogInformation("Remind channel is not set, falling back on default");
                    channel = properties.PingChannel;
                    if (string.IsNullOrEmpty(channel))
                        throw new Exception("Neither remind, not default ping channel is set");
                }
            }
            else
            {
                _logger.LogInformation("Pinging main event {0}/[{1}]", guildId, se.Id);
                channel = properties.PingChannel;
                if (string.IsNullOrEmpty(channel))
                    throw new Exception("Default ping channel is not set");
            }
            await _messagingService.PingChannel(guildId, channel, message);
        }


    }
}
