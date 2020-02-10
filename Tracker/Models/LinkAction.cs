using System;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class LinkAction
    {
        public int Id { get; set; }
        public int PingId { get; set; }
        public Guid LinkId { get; set; }
        public DateTime When { get; set; }
        public string Origin { get; set; }
        public string UserAgent { get; set; }
        public string Data { get; set; }

        public bool IsView
        {
            get
            {
                return Data == "view";
            }
        }
    }
}
