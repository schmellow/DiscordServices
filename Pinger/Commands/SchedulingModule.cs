using Discord;
using Discord.Commands;
using Schmellow.DiscordServices.Pinger.Services;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    public sealed class SchedulingModule : ModuleBase
    {
        ILogger _logger;
        Configuration _configuration;
        SchedulingService _schedulingService;

        public SchedulingModule(ILogger logger, Configuration configuration, SchedulingService schedulingService)
        {
            _logger = logger;
            _configuration = configuration;
            _schedulingService = schedulingService;
        }

        [Command("schedule")]
        [Summary("Schedules an event")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> ScheduleEvent(string date, string time, [Remainder] string message)
        {
            try
            {
                var parsedDate = ParseDate(date, time);
                int id = _schedulingService.ScheduleEvent(Context.Guild.Id, parsedDate, Context.User.Username, message);
                await ReplyAsync(string.Format("Added event [{0}] at {1}", id, parsedDate.ToString("dd.MM.yyyy HH:mm")));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.ToString());
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("repeat")]
        [Summary("Makes a copy of an event. If time is empty - reuse source event time")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> RepeatEvent(int eventId, string date = "", string time = "")
        {
            try
            {
                DateTime parsedDate = ParseDate(date, time);
                ScheduledEvent se = _schedulingService.GetEventById(Context.Guild.Id, eventId);
                if (parsedDate.Date == DateTime.MinValue.Date)
                    parsedDate = se.TargetDate.Date + parsedDate.TimeOfDay;
                if (parsedDate.TimeOfDay == TimeSpan.Zero)
                    parsedDate = parsedDate.Date + se.TargetDate.TimeOfDay;
                int id = _schedulingService.ScheduleEvent(Context.Guild.Id, parsedDate, Context.User.Username, se.Message);
                await ReplyAsync(string.Format("Copied event [{0}] as [{1}] at {2}", eventId, id, parsedDate.ToString("dd.MM.yyyy HH:mm")));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.ToString());
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("remind")]
        [Summary("Pings a reminder about existing event")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> RemindEvent([Remainder] string query)
        {
            try
            {
                await _schedulingService.RemindAsync(Context.Guild.Id, query);
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("cancel")]
        [Summary("Cancels an event")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> CancelEvent(int eventId)
        {
            try
            {
                _schedulingService.CancelEvent(Context.Guild.Id, eventId);
                await ReplyAsync(string.Format("Cancelled event [{0}]", eventId));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("event")]
        [Summary("Shows event details with reminders (if any)")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> ShowEvent(int eventId)
        {
            try
            {
                ScheduledEvent se = _schedulingService.GetEventById(Context.Guild.Id, eventId);
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
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("events")]
        [Summary("Lists last [10] [pending]/all/cancelled/passed events")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> ListEvents(string query = "pending", int limit = 10)
        {
            try
            {
                query = query.ToLowerInvariant();
                EventState? state;
                switch(query)
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
                        throw new Exception(string.Format("Unknown query '{0}'", query));
                }
                var sb = new StringBuilder();
                foreach(var se in _schedulingService.GetEvents(Context.Guild.Id, state, limit))
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
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("change-message")]
        [Summary("Changes event message")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> SetEventMessage(int eventId, [Remainder] string newMessage)
        {
            try
            {
                _schedulingService.UpdateEventMessage(Context.Guild.Id, eventId, newMessage);
                await ReplyAsync(string.Format("Updated message for event [{0}]", eventId));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("change-date")]
        [Summary("Changes event date and time")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> SetEventDate(int eventId, string newDate = "", string newTime = "")
        {
            try
            {
                DateTime parsedDate = ParseDate(newDate, newTime);
                ScheduledEvent se = _schedulingService.GetEventById(Context.Guild.Id, eventId);
                if (parsedDate.Date == DateTime.MinValue.Date)
                    parsedDate = se.TargetDate.Date + parsedDate.TimeOfDay;
                if (parsedDate.TimeOfDay == TimeSpan.Zero)
                    parsedDate = parsedDate.Date + se.TargetDate.TimeOfDay;
                if(parsedDate == se.TargetDate)
                {
                    await ReplyAsync("New date is the same, nothing to change");
                }
                else
                {
                    _schedulingService.UpdateEventDate(Context.Guild.Id, eventId, parsedDate);
                    await ReplyAsync(string.Format(
                        "Moved event [{0}] from {1} to {2}", 
                        eventId,
                        se.TargetDate.ToString("dd.MM.yyyy HH:mm"),
                        parsedDate.ToString("dd.MM.yyyy HH:mm")));
                }
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        [Command("change-reminders")]
        [Summary("Changes event reminders")]
        [RequireContext(ContextType.Guild)]
        [RequireChannel("ControlChannels")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Access")]
        [RequireUser("ElevatedUsers", Group = "Access")]
        [RequireUser("PingUsers", Group = "Access")]
        public async Task<RuntimeResult> SetEventReminders(int eventId, params string[] offsets)
        {
            try
            {
                _schedulingService.UpdateEventReminders(Context.Guild.Id, eventId, offsets);
                await ReplyAsync(string.Format("Updated reminders event [{0}]", eventId));
                return CommandResult.FromSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return CommandResult.FromError(ex.Message);
            }
        }

        private DateTime ParseDate(string left, string right)
        {
            string initialInput = left + " " + right;
            if(string.IsNullOrEmpty(left)) // Nothing was passed
            {
                left = "01.01.0001";
                right = "00:00";
            }
            else if(string.IsNullOrEmpty(right)) // Only one part was passed
            {
                // If left is time - swap and assign minvalue to date part
                // Otherwise assign minvalue to time part
                if (left.Contains(":"))
                {
                    right = left;
                    left = "01.01.0001";
                }
                else // left is date
                {
                    right = "00:00";
                }
            }
            DateTime result;
            string full = left + " " + right;
            bool success = DateTime.TryParseExact(
                full,
                "dd.MM.yyyy HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out result);
            if (success)
                return result;
            throw new FormatException(string.Format("'{0}' - Invalid format, expecting 'dd.MM.yyyy HH:mm' (24h)", initialInput));
        }
    }

}
