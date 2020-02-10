using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Pinger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public sealed partial class SchedulingService
    {
        public int ScheduleEvent(
            ulong guildId,
            string user,
            string message,
            DateTime date)
        {
            if (!IsRunning)
                throw new InvalidOperationException("Scheduling Service is not available");

            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message is empty");

            if (DateTime.Now.ToUniversalTime() > date)
            {
                throw new ArgumentException(string.Format(
                    "Event date '{0}' is in the past",
                    date.ToString("dd.MM.yyyy HH:mm")));
            }

            var guildProperties = _guildPropertyStorage.EnsureGuildProperties(guildId);
            ScheduledEvent newEvent = new ScheduledEvent()
            {
                TargetDate = date,
                User = user,
                Message = message,
                PingOffsets = ParseOffsets(guildProperties.RemindOffsets),
                State = EventState.Pending
            };
            int id = _eventStorage.InsertEvent(guildId, newEvent);
            _logger.LogInformation("Created event {0}/[{1}]", guildId, id);
            Task.Run(() => EventAdded(guildId, newEvent));
            return id;
        }

        // Find directly by id or earliest active by message
        public ScheduledEvent FindEventByIdOrText(ulong guildId, string query = null, bool searchPending = true)
        {
            if (!IsRunning)
                throw new InvalidOperationException("Scheduling Service is not available");

            ScheduledEvent se = null;

            int eventId;
            if (int.TryParse(query, out eventId))
            {
                se = _eventStorage.GetEventById(guildId, eventId);
            }

            if (se == null && searchPending)
            {
                se = _eventStorage.FindGuildEvents(guildId, query, EventState.Pending)
                    .OrderBy(e => e.TargetDate)
                    .FirstOrDefault();
            }

            if (se == null)
                throw new ArgumentException(string.Format("No event found for query '{0}'", query));

            return se;
        }

        public ScheduledEvent[] GetEvents(ulong guildId, EventState? state = null, int limit = 0)
        {
            if (!IsRunning)
                throw new InvalidOperationException("Scheduling Service is not available");

            return _eventStorage.FindGuildEvents(guildId, null, state, limit);
        }

        public int ChangeEvent(
            ulong guildId,
            string query,
            string newMessage,
            DateTime? newDate,
            TimeSpan? newTime,
            string offsets)
        {
            if (!IsRunning)
                throw new InvalidOperationException("Scheduling Service is not available");

            ScheduledEvent se = FindEventByIdOrText(guildId, query);

            if (se.State != EventState.Pending)
                throw new InvalidOperationException("Modifying cancelled or passed events is forbidden");

            DateTime targetDate = newDate.HasValue ? newDate.Value : se.TargetDate.Date;
            if (newTime.HasValue)
                targetDate += newTime.Value;
            else
                targetDate += se.TargetDate.TimeOfDay;

            if (targetDate != se.TargetDate)
            {
                if (DateTime.Now.ToUniversalTime() > targetDate)
                {
                    throw new ArgumentException(string.Format(
                        "New event date '{0}' is in the past",
                        targetDate.ToString("dd.MM.yyyy HH:mm")));
                }
                se.TargetDate = targetDate;
                _logger.LogInformation("Updating event {0}/[{1}] date", guildId, se.Id);
            }
            if (!string.IsNullOrEmpty(newMessage))
            {
                se.Message = newMessage;
                _logger.LogInformation("Updating event {0}/[{1}] message", guildId, se.Id);
            }
            if (offsets != null)
            {
                se.PingOffsets = ParseOffsets(offsets);
                _logger.LogInformation("Updating event {0}/[{1}] reminders", guildId, se.Id);
            }
            if (_eventStorage.UpdateEvent(guildId, se))
            {
                _logger.LogInformation("Updated event {0}/[{1}]", guildId, se.Id);
                Task.Run(() => EventChanged(guildId, se));
                return se.Id;
            }
            throw new Exception(string.Format("Unable to update event [{0}]", se.Id));
        }

        public int CancelEvent(ulong guildId, string query)
        {
            if (!IsRunning)
                throw new InvalidOperationException("Scheduling Service is not available");

            ScheduledEvent se = FindEventByIdOrText(guildId, query);
            if (se.State != EventState.Pending)
                throw new ArgumentException("Changing status of cancelled or passed events is forbidden");

            se.State = EventState.Cancelled;
            if (_eventStorage.UpdateEvent(guildId, se))
            {
                _logger.LogInformation("Cancelled event {0}/[{1}]", guildId, se.Id);
                Task.Run(() => EventCancelled(guildId, se));
                return se.Id;
            }
            throw new Exception(string.Format("Unable to cancel event [{0}]", se.Id));
        }

        public async Task RemindAsync(ulong guildId, string query)
        {
            if (!IsRunning)
                throw new InvalidOperationException("Scheduling Service is not available");

            ScheduledEvent se = FindEventByIdOrText(guildId, query);
            if (se.State != EventState.Pending)
                throw new ArgumentException("Pinging reminders for cancelled or passed events is forbidden");

            await PingEventAsync(guildId, DateTime.Now.ToUniversalTime(), se);
        }

        public void GetNextPingInfo(ulong guildId, out DateTime? date, out ScheduledEvent[] events)
        {
            if (!IsRunning)
                throw new InvalidOperationException("Scheduling Service is not available");

            date = null;
            events = null;

            if (_nextPing != null && _nextPing.Events.ContainsKey(guildId))
            {
                date = _nextPing.Date;
                events = _nextPing.Events[guildId].Select(kv => kv.Value).OrderBy(e => e.Id).ToArray();
            }
        }

        Regex _offsetRegex = new Regex(@"(\d+d)?(\d+h)?(\d+m)?(\d+s)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private TimeSpan[] ParseOffsets(string offsets)
        {
            HashSet<TimeSpan> spans = new HashSet<TimeSpan>();
            spans.Add(TimeSpan.Zero);
            if (!string.IsNullOrEmpty(offsets))
            {
                foreach (string offset in offsets.Split(';'))
                {
                    if (string.IsNullOrEmpty(offset))
                        continue;

                    Match match = _offsetRegex.Match(offset);
                    while (match != null && match.Success)
                    {
                        int days = 0;
                        int hours = 0;
                        int minutes = 0;
                        int seconds = 0;
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            Group group = match.Groups[i];
                            if (!group.Success)
                                continue;
                            var lastChar = group.Value.Last();
                            var value = group.Value.TrimEnd(lastChar);
                            switch (lastChar)
                            {
                                case 'd':
                                    int.TryParse(value, out days);
                                    break;
                                case 'h':
                                    int.TryParse(value, out hours);
                                    break;
                                case 'm':
                                    int.TryParse(value, out minutes);
                                    break;
                                case 's':
                                    int.TryParse(value, out seconds);
                                    break;
                                default:
                                    break;
                            }
                        }
                        spans.Add(TimeSpan.FromDays(days)
                            + TimeSpan.FromHours(hours)
                            + TimeSpan.FromMinutes(minutes)
                            + TimeSpan.FromSeconds(seconds));
                        match = match.NextMatch();
                    }
                }
            }
            return spans.OrderByDescending(s => s).ToArray();
        }

    }
}
