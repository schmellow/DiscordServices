using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class UsersVM
    {
        public sealed class User
        {
            public string Name { get; set; }
            public int LinksCount { get; set; }
            public int ViewsCount { get; set; }
            public bool IsSuspicious { get; set; }
        }

        public List<User> Users { get; private set; }

        public UsersVM()
        {
            Users = new List<User>();
        }
    }
}
