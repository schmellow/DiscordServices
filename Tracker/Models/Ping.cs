using System;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class Ping
    {
        public int Id { get; set; }
        public string Guild { get; set; }
        public string Author { get; set; }
        public DateTime Created { get; set; }
        public string Text { get; set; }

        public string AuthorShort()
        {
            int i = Author.IndexOf('#');
            if(i > 0)
                return Author.Substring(0, i);
            return Author;
        }
    }
}
