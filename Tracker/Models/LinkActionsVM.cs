using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class LinkActionsVM
    {
        public sealed class Action
        {
            public string When { get; set; }
            public string Origin { get; set; }
            public string UserAgent { get; set; }
            public string Data { get; set; }
            public bool IsSuspicious { get; set; }
        }

        public int PingId { get; set; }
        public Guid LinkId { get; set; }
        public string PingCreated { get; set; }
        public string PingAuthor { get; set; }
        public string PingText { get; set; }
        public string User { get; set; }
        public bool HasMultipleOrigins { get; set; }

        public List<Action> Actions { get; private set; }

        public LinkActionsVM()
        {
            Actions = new List<Action>();
        }
    }
}
