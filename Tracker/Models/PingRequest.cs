using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class PingRequest
    {
        public string Guild { get; set; }
        public string Author { get; set; }
        public string Text { get; set; }
        public HashSet<string> Users { get; set; }

        public PingRequest()
        {
            Users = new HashSet<string>();
        }

        public PingRequest(IEnumerable<string> users)
        {
            Users = new HashSet<string>(users);
        }
    }
}
