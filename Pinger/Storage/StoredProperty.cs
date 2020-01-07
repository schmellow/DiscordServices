using System.Collections.Generic;

namespace Schmellow.DiscordServices.Pinger.Storage
{
    public sealed class StoredProperty
    {
        public string Id { get; set; }

        public HashSet<string> Values { get; set; }

        public StoredProperty()
        {
            Values = new HashSet<string>();
        }
    }
}
