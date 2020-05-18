using System;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class Link
    {
        public Guid Id { get; set; }
        public int PingId { get; set; }
        public string Guild { get; set; }
        public string User { get; set; }

        public Link()
        {
            Id = Guid.NewGuid();
        }
    }
}
