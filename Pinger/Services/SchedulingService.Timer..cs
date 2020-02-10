using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Pinger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public sealed partial class SchedulingService
    {
        private void EventAdded(ulong guildId, ScheduledEvent se)
        {
            lock (_sync)
            {
                try
                {
                    DateTime earliest = se.ActualPingDates.First();

                    // If ping is not present - create
                    // Else if ping is present:
                    //   If ping is later than event earliest date - recreate ping with added event
                    //   If ping date is the same with event earliest date - add/update event in ping
                    if (_nextPing == null || earliest < _nextPing.Date)
                    {
                        _nextPing = new ScheduledPing(earliest);
                        _nextPing.SetEvent(guildId, se);
                        RecycleTimer();
                    }
                    else if (_nextPing != null && earliest == _nextPing.Date)
                    {
                        _nextPing.SetEvent(guildId, se);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }

        private void EventChanged(ulong guildId, ScheduledEvent se)
        {
            lock (_sync)
            {
                try
                {
                    DateTime earliest = se.ActualPingDates.First();

                    // If ping is not present - create
                    // Else if ping is present:
                    //   If ping is later than event earliest date - recreate ping with updated event
                    //   If ping date is the same with event earliest date - add/update event in ping
                    //   If ping contains event and event earliest date is later - remove it from ping
                    if (_nextPing == null || earliest < _nextPing.Date)
                    {
                        _nextPing = new ScheduledPing(earliest);
                        _nextPing.SetEvent(guildId, se);
                        RecycleTimer();
                    }
                    else if (_nextPing != null)
                    {
                        if (earliest == _nextPing.Date)
                        {
                            _nextPing.SetEvent(guildId, se);
                        }
                        else if (_nextPing.HasEvent(guildId, se) && earliest > _nextPing.Date)
                        {
                            _nextPing.RemoveEvent(guildId, se);
                            if (_nextPing.IsEmpty)
                                FullUpdate();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }

        private void EventCancelled(ulong guildId, ScheduledEvent se)
        {
            lock (_sync)
            {
                try
                {
                    if (_nextPing != null && _nextPing.HasEvent(guildId, se))
                    {
                        _nextPing.RemoveEvent(guildId, se);
                        if (_nextPing.IsEmpty)
                            FullUpdate();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }

        private void FullUpdate()
        {
            try
            {
                // Build full timeline out of active events
                var timeline = new Dictionary<DateTime, Dictionary<ulong, Dictionary<int, ScheduledEvent>>>();
                var activeEventMap = _eventStorage.FindAllEvents(null, EventState.Pending);
                foreach (var kv in activeEventMap)
                {
                    ulong guildId = kv.Key;
                    ScheduledEvent[] events = kv.Value;
                    foreach (ScheduledEvent se in events)
                    {
                        if (!VerifyEvent(guildId, se))
                            continue;
                        foreach (DateTime date in se.ActualPingDates)
                        {
                            Dictionary<ulong, Dictionary<int, ScheduledEvent>> guildEventMap;
                            if (!timeline.TryGetValue(date, out guildEventMap))
                            {
                                guildEventMap = new Dictionary<ulong, Dictionary<int, ScheduledEvent>>();
                                timeline[date] = guildEventMap;
                            }
                            Dictionary<int, ScheduledEvent> eventMap;
                            if (!guildEventMap.TryGetValue(guildId, out eventMap))
                            {
                                eventMap = new Dictionary<int, ScheduledEvent>();
                                guildEventMap[guildId] = eventMap;
                            }
                            eventMap[se.Id] = se;
                        }
                    }
                }
                //
                if (timeline.Any())
                {
                    var earliest = timeline.OrderBy(kv => kv.Key).First();
                    DateTime date = earliest.Key;
                    Dictionary<ulong, Dictionary<int, ScheduledEvent>> events = earliest.Value;
                    _nextPing = new ScheduledPing(date, events);
                }
                else
                {
                    _nextPing = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                _nextPing = null;
            }
            RecycleTimer();
        }

        private bool VerifyEvent(ulong guildId, ScheduledEvent se)
        {
            if (DateTime.Now.ToUniversalTime() > se.TargetDate)
            {
                _logger.LogWarning("{0}/[{1}] has passed while service was offline, fixing", guildId, se.Id);
                se.State = EventState.Passed;
                _eventStorage.UpdateEvent(guildId, se);
                return false;
            }
            return true;
        }

        private void RecycleTimer(bool stop = false)
        {
            _logger.LogInformation("{0} timer", stop ? "Stopping" : "Recycling");
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            if (stop)
                return;
            if (_nextPing == null)
            {
                _logger.LogInformation("No next event");
                return;
            }

            TimeSpan interval = _nextPing.Date - DateTime.Now.ToUniversalTime();
            double intervalMS = interval.TotalMilliseconds;
            if (intervalMS <= 0)
            {
                _logger.LogError("Calculated interval {0} <= 0", interval);
                return;
            }
            else if (intervalMS > int.MaxValue)
            {
                _logger.LogWarning("Calculated interval is bigger than max wait time, gating");
                intervalMS = int.MaxValue;
            }
            _timer = new Timer(intervalMS);
            _timer.AutoReset = false;
            _timer.Enabled = true;
            _timer.Elapsed += HandleElapsed;
            _timer.Start();
            _logger.LogInformation(
                "Next event at {0}ET - in {1}",
                _nextPing.Date.ToString("dd.MM.yyyy HH:mm"),
                interval);
        }

        private void HandleElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!IsRunning)
                    throw new InvalidOperationException("Scheduling Service is not available");

                if (_nextPing == null)
                    throw new InvalidOperationException("Ping event is not available");

                DateTime now = DateTime.Now.ToUniversalTime();

                // Detect gated interval
                if (_timer.Interval == int.MaxValue && (_nextPing.Date - now).TotalMilliseconds > 50)
                {
                    _logger.LogInformation("Reached the end of gated wait interval");
                    return;
                }

                if (_nextPing.IsEmpty)
                    throw new InvalidOperationException("Empty ping");

                // Ping events
                foreach (var kv in _nextPing.Events)
                {
                    var guildId = kv.Key;
                    foreach (ScheduledEvent se in kv.Value.Values)
                    {
                        try
                        {
                            PingEventAsync(guildId, _nextPing.Date, se).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                        finally
                        {
                            if (now > se.TargetDate)
                            {
                                _logger.LogInformation("Setting PASSED status for event {0}/[{1}]", kv.Key, se.Id);
                                se.State = EventState.Passed;
                                _eventStorage.UpdateEvent(guildId, se);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
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
