using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class PingLinksVM
    {
        public sealed class Link
        {
            public Guid Id { get; set; }
            public string User { get; set; }
            public int ViewsCount { get; set; }
            public bool HasSuspiciousActions { get; set; }
        }

        public int PingId { get; set; }
        public string PingCreated { get; set; }
        public string PingAuthor { get; set; }
        public string PingText { get; set; }

        public List<Link> Links { get; private set; }
        public Dictionary<string, Guid> MultipleOriginUsers { get; private set; }

        public PingLinksVM()
        {
            Links = new List<Link>();
            MultipleOriginUsers = new Dictionary<string, Guid>();
        }
    }
}
