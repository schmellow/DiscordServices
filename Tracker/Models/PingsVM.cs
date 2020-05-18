using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class PingsVM
    {
        public sealed class Ping
        {
            public int Id { get; set; }
            public string Created { get; set; }
            public string Author { get; set; }
            public string Text { get; set; }
            public int UserCount { get; set; }
            public int ViewsCount { get; set; }
            public bool HasSuspiciousActions { get; set; }
        }

        public List<Ping> Pings { get; private set; }

        public PingsVM()
        {
            Pings = new List<Ping>();
        }
    }
}
