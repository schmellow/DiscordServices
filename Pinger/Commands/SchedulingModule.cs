using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Pinger.Models;
using Schmellow.DiscordServices.Pinger.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class SchedulingModule : ModuleBase
    {
        ILogger<SchedulingModule> _logger;
        SchedulingService _schedulingService;

        public SchedulingModule(
            ILogger<SchedulingModule> logger, 
            SchedulingService schedulingService)
        {
            _logger = logger;
            _schedulingService = schedulingService;
        }

        [Command("schedule")]
        [Summary("Schedules an event")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> ScheduleEvent(string date, string time, [Remainder] string message)
        {
            try
            {
                DateTime? parsedDate;
                TimeSpan? parsedTime;
                ParseDateTime(date, time, out parsedDate, out parsedTime);
                if (!parsedDate.HasValue || !parsedTime.HasValue)
                    throw new ArgumentException("Expecting both date and time to schedule new event");

                DateTime targetDate = parsedDate.Value + parsedTime.Value;

                int id = _schedulingService.ScheduleEvent(Context.Guild.Id, Context.User.Username, message, targetDate);
                await ReplyAsync(string.Format("Added event [{0}] at {1}", id, targetDate.ToString("dd.MM.yyyy HH:mm")));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.ToString());
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("repeat")]
        [Summary("Makes a copy of an event. If time is empty - reuse source event time")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> RepeatEvent(string query, string date = "", string time = "")
        {
            try
            {
                DateTime? parsedDate;
                TimeSpan? parsedTime;
                ParseDateTime(date, time, out parsedDate, out parsedTime);
                if (!parsedDate.HasValue && !parsedTime.HasValue)
                    throw new ArgumentException("Expecting at least new date or new time to repeat event");

                ScheduledEvent se = _schedulingService.FindEventByIdOrText(Context.Guild.Id, query);

                DateTime targetDate = parsedDate.HasValue ? parsedDate.Value : se.TargetDate.Date;
                if (parsedTime.HasValue)
                    targetDate += parsedTime.Value;
                else
                    targetDate += se.TargetDate.TimeOfDay;

                int id = _schedulingService.ScheduleEvent(Context.Guild.Id, Context.User.Username, se.Message, targetDate);
                await ReplyAsync(string.Format("Copied event [{0}] as [{1}] at {2}", se.Id, id, targetDate.ToString("dd.MM.yyyy HH:mm")));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.ToString());
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("remind")]
        [Summary("Pings a reminder about existing event")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> RemindEvent([Remainder] string query = "")
        {
            try
            {
                await _schedulingService.RemindAsync(Context.Guild.Id, query);
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("cancel")]
        [Summary("Cancels an event")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> CancelEvent(string query = "")
        {
            try
            {
                int id = _schedulingService.CancelEvent(Context.Guild.Id, query);
                await ReplyAsync(string.Format("Cancelled event [{0}]", id));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("event")]
        [Summary("Shows event details with reminders (if any)")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> ShowEvent(string query = "")
        {
            try
            {
                ScheduledEvent se = _schedulingService.FindEventByIdOrText(Context.Guild.Id, query);
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.Title = string.Format("[{0}] {1}", se.Id, se.ETA);
                embedBuilder.AddField("From " + se.User, se.Message);
                var reminders = se.ReminderETAs;
                if (reminders.Length > 0)
                {
                    embedBuilder.AddField(
                        "Reminders at:",
                        string.Join("\n", reminders));
                }
                await ReplyAsync(string.Empty, false, embedBuilder.Build());
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("events")]
        [Summary("Lists last [10] [pending]/all/cancelled/passed events. Or next ping with 'status' or 'state',")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> ListEvents(string query = "pending", int limit = 10)
        {
            try
            {
                query = query.ToLowerInvariant();
                if(query == "status" || query == "state")
                {
                    DateTime? date;
                    ScheduledEvent[] events;
                    _schedulingService.GetNextPingInfo(Context.Guild.Id, out date, out events);
                    if(!date.HasValue)
                    {
                        await ReplyAsync("No ping events pending");
                    }
                    else
                    {
                        EmbedBuilder embedBuilder = new EmbedBuilder();
                        embedBuilder.Title = "Next ping event - " + date.Value.ToString("dd.MM.yyyy HH:mm");
                        // Use only two embed fields: reminders and main events
                        List<string> reminders = new List<string>();
                        List<string> main = new List<string>();
                        foreach (ScheduledEvent se in events)
                        {
                            string value = string.Format("[{0}] at {1} from {2}", se.Id, se.TargetDateString, se.User);
                            if (date.Value < se.TargetDate)
                                reminders.Add(value);
                            else
                                main.Add(value);
                        }
                        if(reminders.Any())
                        {
                            embedBuilder.AddField(
                                "Reminder for",
                                string.Join("\n", reminders));
                        }
                        if(main.Any())
                        {
                            embedBuilder.AddField(
                                "Main event for",
                                string.Join("\n", main));
                        }
                        await ReplyAsync(string.Empty, false, embedBuilder.Build());
                    }
                }
                else
                {
                    EventState? state;
                    switch (query)
                    {
                        case "pending":
                            state = EventState.Pending;
                            break;
                        case "all":
                            state = null;
                            break;
                        case "cancelled":
                            state = EventState.Cancelled;
                            break;
                        case "passed":
                            state = EventState.Passed;
                            break;
                        default:
                            throw new ArgumentException(string.Format("Invalid query type '{0}'", query));
                    }
                    var sb = new StringBuilder();
                    foreach (var se in _schedulingService.GetEvents(Context.Guild.Id, state, limit))
                    {
                        sb.Append("----------\n");
                        sb.AppendFormat("[{0}] {1} - From {2}\n", se.Id, se.ETA, se.User);
                        sb.Append(Format.Code(se.Message) + "\n");
                    }
                    if (sb.Length == 0)
                    {
                        await ReplyAsync("No events found");
                    }
                    else
                    {
                        await ReplyAsync(sb.ToString());
                    }
                }
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("change-message")]
        [Summary("Changes event message")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> ChangeEventMessage(string query, [Remainder] string message)
        {
            try
            {
                int id = _schedulingService.ChangeEvent(Context.Guild.Id, query, message, null, null, null);
                await ReplyAsync(string.Format("Updated message for event [{0}]", id));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("change-date")]
        [Summary("Changes event date and time")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> SetEventDate(string query, string date = "", string time = "")
        {
            try
            {
                DateTime? parsedDate;
                TimeSpan? parsedTime;
                ParseDateTime(date, time, out parsedDate, out parsedTime);
                if (!parsedDate.HasValue && !parsedTime.HasValue)
                    throw new ArgumentException("Expecting at least new date or new time to repeat event");

                int id = _schedulingService.ChangeEvent(Context.Guild.Id, query, null, parsedDate, parsedTime, null);
                await ReplyAsync(string.Format("Rescheduled event [{0}] to {1}", id, string.Join(" ", date, time)));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("change-reminders")]
        [Summary("Changes event reminders")]
        [RequireContext(ContextType.Guild)]
        [RequireControlChannel]
        [RequirePermissionLevel(PermissionLevel.Pings)]
        public async Task<RuntimeResult> SetEventReminders(string query, params string[] offsets)
        {
            try
            {
                string offsetsValue = "";
                if (offsets.Any())
                    offsetsValue = string.Join(";", offsets);
                int id = _schedulingService.ChangeEvent(Context.Guild.Id, query, null, null, null, offsetsValue);
                await ReplyAsync(string.Format("Updated reminders event [{0}]", id));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        private void ParseDateTime(string left, string right, out DateTime? date, out TimeSpan? time)
        {
            date = null;
            time = null;
            // Nothing was passed - exit
            if (string.IsNullOrEmpty(left))
                return;
            // Only one string is passed
            if(string.IsNullOrEmpty(right)) // Only one part was passed
            {
                // If left is time - swap
                if(left.Contains(":"))
                {
                    right = left;
                    left = string.Empty;
                }
            }
            bool success = false;
            // try parse date
            if(!string.IsNullOrEmpty(left))
            {
                DateTime dateValue;
                success = DateTime.TryParseExact(
                    left,
                    "dd.MM.yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out dateValue);
                if (success)
                    date = dateValue;
            }
            // try parse time
            if(!string.IsNullOrEmpty(right))
            {
                TimeSpan timeValue;
                success = TimeSpan.TryParseExact(
                    right,
                    @"hh\:mm",
                    CultureInfo.InvariantCulture,
                    out timeValue);
                if (success)
                    time = timeValue;
            }
            if(!success)
            {
                throw new FormatException(string.Format(
                    "'{0}' - Invalid format, expecting 'dd.MM.yyyy HH:mm' (24h)", 
                    left + right));
            }   
        }

    }

}
