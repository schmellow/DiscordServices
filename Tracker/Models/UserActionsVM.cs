using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class UserActionsVM
    {
        public sealed class Action
        {
            public int PingId { get; set; }
            public Guid LinkId { get; set; }
            public string When { get; set; }
            public string Origin { get; set; }
            public string UserAgent { get; set; }
            public string Data { get; set; }
            public bool IsSuspicious { get; set; }
        }

        public string UserName { get; set; }
        public List<Action> Actions { get; private set; }
        public Dictionary<int, Guid> MultipleOriginLinks { get; private set; }

        public UserActionsVM()
        {
            Actions = new List<Action>();
            MultipleOriginLinks = new Dictionary<int, Guid>();
        }
    }
}
