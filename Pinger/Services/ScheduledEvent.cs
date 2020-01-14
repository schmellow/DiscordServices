using System;
using System.Collections.Generic;
using System.Linq;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public enum EventState
    {
        Pending,
        Passed,
        Cancelled
    };

    public sealed class ScheduledEvent
    {
        public int Id { get; set; }
        public string User { get; set; }
        public string Message { get; set; }
        public DateTime TargetDate { get; set; }
        public TimeSpan[] PingOffsets { get; set; }
        public EventState State { get; set; }

        public string TargetDateString
        {
            get
            {
                return TargetDate.ToString("dd.MM.yyyy HH:mm");
            }
        }

        /// <summary>
        /// Returns all pending ping dates for the event, including target date
        /// </summary>
        public DateTime[] ActualPingDates
        {
            get
            {
                var now = DateTime.Now.ToUniversalTime();
                return PingOffsets.Select(offset => TargetDate - offset)
                    .Where(date => date > now)
                    .OrderBy(date => date).ToArray();
            }
        }

        /// <summary>
        /// Returns ETA info strings for pending reminders (excluding event date)
        /// </summary>
        public string[] ReminderETAs
        {
            get
            {
                var now = DateTime.Now.ToUniversalTime();
                return ActualPingDates
                    .Where(date => date != TargetDate)
                    .Select(date =>
                    {
                        return string.Format(
                            "{0} - in {1}",
                            date.ToString("dd.MM.yyyy HH:mm"),
                            (date - now).ToString(@"dd\.hh\:mm\:ss"));
                    }).ToArray();
            }
        }

        /// <summary>
        /// Return ETA info string for and event
        /// </summary>
        public string ETA
        {
            get
            {
                if (State == EventState.Passed)
                    return string.Format("{0} - PASSED", TargetDateString);
                else if (State == EventState.Cancelled)
                    return string.Format("{0} - CANCELLED", TargetDateString);
                TimeSpan span = TargetDate - DateTime.Now.ToUniversalTime();
                if (span.Days >= 1)
                    return string.Format("{0} - in {1} day(s)", TargetDateString, Math.Round(span.TotalDays));
                else if (span.Hours >= 1)
                    return string.Format("{0} - in {1} hour(s)", TargetDateString, Math.Round(span.TotalHours));
                else if (span.Minutes >= 1)
                    return string.Format("{0} - in {1} minute(s)", TargetDateString, Math.Round(span.TotalMinutes));
                else if (span.Seconds > 10)
                    return string.Format("{0} - in {1} second(s)", TargetDateString, Math.Round(span.TotalSeconds));
                else
                    return string.Format("{0} - NOW", TargetDateString);
            }
        }

    }
}
