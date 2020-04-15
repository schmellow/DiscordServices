using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class HistoryPingVM
    {
        public class HistoryLink
        {
            public Guid Id { get; set; }
            public string User { get; set; }
            public int ActionCount { get; set; }
            public int ViewsCount { get; set; }
            public int OtherCount { get; set; }
        }

        public int Id { get; set; }
        public DateTime Created { get; set; }
        public string Guild { get; set; }
        public string Author { get; set; }
        public string Text { get; set; }
        public int UserCount { get; set; }
        public int ActionCount { get; set; }
        public int ViewsCount { get; set; }
        public int OtherCount { get; set; }

        public List<HistoryLink> Links = new List<HistoryLink>();

        public HistoryPingVM()
        {
            Links = new List<HistoryLink>();
        }
    }
}
