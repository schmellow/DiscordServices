using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public class HistoryTimelineVM
    {
        public class HistoryAction
        {
            public string User { get; set; }
            public LinkAction Action { get; set; }
        }

        public int PingId { get; set; }
        public DateTime PingCreated { get; set; }
        public string PingGuild { get; set; }
        public string PingAuthor { get; set; }
        public string PingText { get; set; }
        public int ActionCount { get; set; }
        public int ViewsCount { get; set; }
        public int OtherCount { get; set; }

        public List<HistoryAction> Actions { get; private set; }

        public HistoryTimelineVM()
        {
            Actions = new List<HistoryAction>();
        }
    }
}
