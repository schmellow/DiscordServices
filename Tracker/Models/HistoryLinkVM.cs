using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public class HistoryLinkVM
    {
        public Guid Id { get; set; }
        public int PingId { get; set; }
        public DateTime PingCreated { get; set; }
        public string PingGuild { get; set; }
        public string PingAuthor { get; set; }
        public string PingText { get; set; }
        public string User { get; set; }
        public int ActionCount { get; set; }
        public int ViewsCount { get; set; }
        public int OtherCount { get; set; }

        public List<LinkAction> Actions { get; private set; }

        public HistoryLinkVM()
        {
            Actions = new List<LinkAction>();
        }
    }
}
