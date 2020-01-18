using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public sealed partial class SchedulingService
    {
        static readonly object _sync = new object();

        readonly ILogger _logger;
        readonly Configuration _configuration;
        readonly LiteDatabase _db;

        Timer _timer;
        PingData _nextPing;

        public bool IsRunning { get; private set; }

        public event Func<DateTime, ulong, ScheduledEvent, Task> PingEvent;

        public SchedulingService(
            ILogger logger, 
            Configuration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _db = new LiteDatabase(string.Format("events-{0}.db", _configuration.InstanceName));
        }

        public void Run()
        {
            lock (_sync)
            {
                _logger.Info("Running scheduler");
                FullUpdate();
                IsRunning = true;
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                _logger.Info("Stopping scheduler");
                RecycleTimer(true);
                _db.Dispose();
                IsRunning = false;
            }
        }

        public int ScheduleEvent(ulong guildId, DateTime date, string user, string message)
        {
            if (!IsRunning)
                throw new Exception("Service is not running");
            if (DateTime.Now.ToUniversalTime() > date)
                throw new Exception(string.Format(
                    "Event date '{0}' is in the past",
                    date.ToString("dd.MM.yyyy HH:mm")));

            // Write event into DB
            var guildProperties = _configuration.GetGuildProperties(guildId);
            ScheduledEvent newEvent = new ScheduledEvent()
            {
                TargetDate = date,
                User = user,
                Message = message,
                PingOffsets = ParseOffsets(guildProperties.RemindOffsets.ToArray()),
                State = EventState.Pending
            };
            var events = _db.GetCollection<ScheduledEvent>(guildId.ToString());
            int eventId = (int)events.Insert(newEvent);
            _logger.Info("Created event {0}/[{1}]", guildId, eventId);
            Task.Run(() => EventAdded(guildId, eventId));
            return eventId;
        }

        public void UpdateEventMessage(ulong guildId, int eventId, string newMessage)
        {
            if (string.IsNullOrEmpty(newMessage))
                throw new Exception("New message is empty");

            var events = _db.GetCollection<ScheduledEvent>(guildId.ToString());
            ScheduledEvent changedEvent = events.FindById(eventId);
            if (changedEvent == null)
                throw new Exception(string.Format("Event [{0}] was not found", eventId));

            if (changedEvent.State != EventState.Pending)
                throw new Exception("Can't modify passed/cancelled event");

            changedEvent.Message = newMessage;
            events.Update(changedEvent);
            _logger.Info("Changed message for event {0}/[{1}]", guildId, eventId);
        }

        public void UpdateEventDate(ulong guildId, int eventId, DateTime newDate)
        {
            if (DateTime.Now.ToUniversalTime() > newDate)
                throw new Exception(string.Format(
                    "New event date '{0}' is in the past",
                    newDate.ToString("dd.MM.yyyy HH:mm")));

            var events = _db.GetCollection<ScheduledEvent>(guildId.ToString());
            ScheduledEvent changedEvent = events.FindById(eventId);
            if (changedEvent == null)
                throw new Exception(string.Format("Event [{0}] was not found", eventId));

            if (changedEvent.State != EventState.Pending)
                throw new Exception("Can't modify passed/cancelled event");

            changedEvent.TargetDate = newDate;
            events.Update(changedEvent);
            _logger.Info("Changed date for event {0}/[{1}]", guildId, eventId);
            Task.Run(() => EventUpdated(guildId, eventId));
        }

        public void UpdateEventReminders(ulong guildId, int eventId, params string[] offsets)
        {
            var events = _db.GetCollection<ScheduledEvent>(guildId.ToString());
            ScheduledEvent changedEvent = events.FindById(eventId);
            if (changedEvent == null)
                throw new Exception(string.Format("Event [{0}] was not found", eventId));

            if (changedEvent.State != EventState.Pending)
                throw new Exception("Can't modify passed/cancelled event");

            changedEvent.PingOffsets = ParseOffsets(offsets);
            events.Update(changedEvent);
            _logger.Info("Changed reminders for event {0}/[{1}]", guildId, eventId);
            Task.Run(() => EventUpdated(guildId, eventId));
        }

        public void CancelEvent(ulong guildId, int eventId)
        {
            if (!IsRunning)
                throw new Exception("Service is not running");

            var events = _db.GetCollection<ScheduledEvent>(guildId.ToString());
            ScheduledEvent cancelledEvent = events.FindById(eventId);
            if (cancelledEvent == null)
                throw new Exception(string.Format("Event [{0}] was not found", eventId));

            if (cancelledEvent.State != EventState.Pending)
                throw new Exception("Can't cancel passed/cancelled event");

            cancelledEvent.State = EventState.Cancelled;
            events.Update(cancelledEvent);
            _logger.Info("Cancelled event {0}/[{1}]", guildId, eventId);
            Task.Run(() => EventCancelled(guildId, eventId));
        }

        public async Task RemindAsync(ulong guildId, string query)
        {
            if (!IsRunning)
                throw new Exception("Service is not running");

            int id;
            if (!int.TryParse(query, out id))
                id = 0;
            string lquery = query.ToLowerInvariant();
            ScheduledEvent se = GetEvents(guildId, EventState.Pending)
                .OrderBy(e => e.TargetDate)
                .Where(e => e.Id == id || e.Message.ToLowerInvariant().Contains(lquery))
                .FirstOrDefault();
            if (se == null)
                throw new Exception(string.Format("Unable to find any pending event by query '{0}'", query));

            await PingEvent(DateTime.Now, guildId, se);
        }

        public PingData GetPingEvent(ulong guildId)
        {
            if(_nextPing != null && _nextPing.EventMap.ContainsKey(guildId))
            {
                return new PingData(
                    _nextPing.Date,
                    _nextPing.EventMap.Where(kv => kv.Key == guildId).ToDictionary(kv => kv.Key, kv => kv.Value));
            }
            return null;
        }

        public ScheduledEvent GetEventById(ulong guildId, int eventId)
        {
            var events = _db.GetCollection<ScheduledEvent>(guildId.ToString());
            ScheduledEvent se = events.FindById(eventId);
            if (se == null)
                throw new Exception(string.Format("Event [{0}] was not found", eventId));
            return se;
        }

        public IEnumerable<ScheduledEvent> GetEvents(ulong guildId, EventState? state = null, int limit = 0)
        {
            var events = _db.GetCollection<ScheduledEvent>(guildId.ToString());
            events.EnsureIndex("State");
            Query query;
            if (state.HasValue)
                query = Query.EQ("State", state.Value.ToString());
            else
                query = Query.All();
            if (limit > 0)
                return events.Find(query).OrderBy(e => e.Id).TakeLast(limit);
            else
                return events.Find(query).OrderBy(e => e.Id);
        }

        Regex _offsetRegex = new Regex(@"(\d+h)?(\d+m)?(\d+s)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private TimeSpan[] ParseOffsets(params string[] offsets)
        {
            HashSet<TimeSpan> spans = new HashSet<TimeSpan>();
            spans.Add(TimeSpan.Zero);
            foreach (string offset in offsets)
            {
                if (!string.IsNullOrEmpty(offset))
                {
                    Match match = _offsetRegex.Match(offset);
                    while (match != null && match.Success)
                    {
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
                        spans.Add(TimeSpan.FromHours(hours)
                            + TimeSpan.FromMinutes(minutes)
                            + TimeSpan.FromSeconds(seconds));
                        match = match.NextMatch();
                    }
                }
            }
            return spans.OrderByDescending(s => s).ToArray();
        }

        private void EventAdded(ulong guildId, int eventId)
        {
            lock(_sync)
            {
                try
                {
                    ScheduledEvent e = GetEventById(guildId, eventId);
                    DateTime earliest = e.ActualPingDates.First();

                    // Set next ping if no next ping is available
                    // If new event earlierst date is earlier than ping date - reset next ping
                    // Else if earliest date is the same as ping date - account for new event
                    if (_nextPing == null)
                    {
                        _nextPing = new PingData(earliest);
                        _nextPing.AddEvent(guildId, eventId);
                        RecycleTimer();
                    }
                    else
                    {
                        if (_nextPing.Date > earliest)
                        {
                            _nextPing = new PingData(earliest);
                            _nextPing.AddEvent(guildId, eventId);
                            RecycleTimer();
                        }
                        else if (_nextPing.Date == earliest)
                        {
                            _nextPing.AddEvent(guildId, eventId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, ex.Message);
                }
            }
        }

        private void EventUpdated(ulong guildId, int eventId)
        {
            lock(_sync)
            {
                try
                {
                    ScheduledEvent e = GetEventById(guildId, eventId);
                    DateTime earliest = e.ActualPingDates.First();

                    // Set ping event if no ping event is present
                    // If ping event is present, check if it contains given scheduled event
                    //  - If contains:
                    //   - If earliest date is earlier than current - replace
                    //   - If earliest date is later than current check how many scheduled events ping event carries.
                    //    - If one - that means the only scheduled event (given) was moved forwards.
                    //      Hence we are forced to do full update, since we don't know which event is earliest now.
                    //    - If more than one - remove given scheduled event and leave the rest
                    //   - Do nothing if date is the same
                    // If present ping event does not contain given scheduled event:
                    //  - If date is the same - account for given scheduled event
                    //  - If date is earlier than present - replace
                    if (_nextPing == null)
                    {
                        _nextPing = new PingData(earliest);
                        _nextPing.AddEvent(guildId, eventId);
                        RecycleTimer();
                    }
                    else if (_nextPing.HasEvent(guildId, eventId))
                    {
                        if (_nextPing.Date > earliest)
                        {
                            _nextPing = new PingData(earliest);
                            _nextPing.AddEvent(guildId, eventId);
                            RecycleTimer();
                        }
                        else if (_nextPing.Date < earliest)
                        {
                            _nextPing.RemoveEvent(guildId, eventId);
                            if (_nextPing.IsEmpty)
                                FullUpdate();
                        }
                    }
                    else if (_nextPing.Date == earliest)
                    {
                        _nextPing.AddEvent(guildId, eventId);
                    }
                    else if (_nextPing.Date > earliest)
                    {
                        _nextPing = new PingData(earliest);
                        _nextPing.AddEvent(guildId, eventId);
                        RecycleTimer();
                    }
                }
                catch(Exception ex)
                {
                    _logger.Error(ex, ex.Message);
                }
            }
        }

        private void EventCancelled(ulong guildId, int eventId)
        {
            lock(_sync)
            {
                try
                {
                    if (_nextPing == null)
                        return;
                    if (_nextPing.HasEvent(guildId, eventId))
                    {
                        _nextPing.RemoveEvent(guildId, eventId);
                        if (_nextPing.IsEmpty)
                            FullUpdate();
                    }
                }
                catch(Exception ex)
                {
                    _logger.Error(ex, ex.Message);
                }
            }
        }

        private void FullUpdate()
        {
            try
            {
                // Build full timeline out of active events
                // Get earliest date
                // build timeline
                var timeline = new Dictionary<DateTime, Dictionary<ulong, HashSet<int>>>();
                foreach (string collectionName in _db.GetCollectionNames())
                {
                    var collection = _db.GetCollection<ScheduledEvent>(collectionName);
                    ulong guildId = ulong.Parse(collection.Name);
                    collection.EnsureIndex("State");
                    var events = collection.Find(Query.EQ("State", EventState.Pending.ToString()));
                    foreach (ScheduledEvent e in events)
                    {
                        if (!VerifyEvent(collection, e))
                            continue;
                        foreach (DateTime date in e.ActualPingDates)
                        {
                            Dictionary<ulong, HashSet<int>> eventMap;
                            if (!timeline.TryGetValue(date, out eventMap))
                            {
                                eventMap = new Dictionary<ulong, HashSet<int>>();
                                timeline[date] = eventMap;
                            }
                            HashSet<int> eventIds;
                            if (!eventMap.TryGetValue(guildId, out eventIds))
                            {
                                eventIds = new HashSet<int>();
                                eventMap[guildId] = eventIds;
                            }
                            eventIds.Add(e.Id);
                        }
                    }
                }
                //
                if (timeline.Any())
                {
                    var earliest = timeline.OrderBy(kv => kv.Key).First();
                    DateTime date = earliest.Key;
                    Dictionary<ulong, HashSet<int>> eventMap = earliest.Value;
                    _nextPing = new PingData(date, eventMap);
                }
                else
                {
                    _nextPing = null;
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex, ex.Message);
                _nextPing = null;
            }
            RecycleTimer();
        }

        private bool VerifyEvent(LiteCollection<ScheduledEvent> collection, ScheduledEvent e)
        {
            if (DateTime.Now.ToUniversalTime() > e.TargetDate)
            {
                _logger.Warn("{0}/[{1}] has passed while service was offline, fixing", collection.Name, e.Id);
                e.State = EventState.Passed;
                collection.Update(e);
                return false;
            }
            return true;
        }

        private void RecycleTimer(bool stop = false)
        {
            _logger.Info("{0} timer", stop ? "Stopping" : "Recycling");
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            if (stop)
                return;
            if(_nextPing == null)
            {
                _logger.Info("No next event");
                return;
            }

            TimeSpan interval = _nextPing.Date - DateTime.Now.ToUniversalTime();
            double intervalMS = interval.TotalMilliseconds;
            if (intervalMS <= 0)
            {
                _logger.Error("Calculated interval {0} <= 0", interval);
                return;
            }
            else if(intervalMS > int.MaxValue)
            {
                _logger.Warn("Calculated interval is bigger than max wait time, gating");
                intervalMS = int.MaxValue;
            }
            _timer = new Timer(intervalMS);
            _timer.AutoReset = false;
            _timer.Enabled = true;
            _timer.Elapsed += HandleElapsed;
            _timer.Start();
            _logger.Info(
                "Next event at {0}ET - in {1}", 
                _nextPing.Date.ToString("dd.MM.yyyy HH:mm"), 
                interval);
        }

        private void HandleElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if(!IsRunning)
                    throw new Exception("Service is not running");

                if (_nextPing == null)
                    throw new Exception("Ping event is not available");

                DateTime now = DateTime.Now.ToUniversalTime();
                // for waiting more than int.MaxValue
                if (now < _nextPing.Date) 
                {
                    _logger.Info("Reached the end of gated wait interval");
                    return;
                }

                if (_nextPing.IsEmpty)
                    throw new Exception("Empty ping");

                // Ping events
                foreach(var kv in _nextPing.EventMap)
                {
                    var guildId = kv.Key;
                    var collection = _db.GetCollection<ScheduledEvent>(guildId.ToString());
                    foreach(int eventId in kv.Value)
                    {
                        ScheduledEvent se = collection.FindById(eventId);
                        if (se == null)
                        {
                            _logger.Error("Event {0}/[{1}] was not found", guildId, eventId);
                            continue;
                        }
                        PingEvent(_nextPing.Date, guildId, se).GetAwaiter().GetResult();
                        if(now > se.TargetDate)
                        {
                            _logger.Info("Setting PASSED status for event {0}/[{1}]", kv.Key, se.Id);
                            se.State = EventState.Passed;
                            collection.Update(se);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
            }
            finally
            {
                lock (_sync)
                {
                    FullUpdate();
                }
            }
        }

    }
}
