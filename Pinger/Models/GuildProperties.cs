using System;
using System.Linq;
using System.Reflection;

namespace Schmellow.DiscordServices.Pinger.Models
{
    public sealed class GuildProperties
    {
        public sealed class GuildPropertyAttribute : Attribute
        {
            public string Value { get; private set; }

            public GuildPropertyAttribute(string value)
            {
                Value = value;
            }

            public static string GetValue(PropertyInfo property)
            {
                var attr = GetCustomAttribute(property, typeof(GuildPropertyAttribute)) as GuildPropertyAttribute;
                if (attr == null)
                    return "";
                return attr.Value;
            }
        }

        public ulong GuildId { get; set; }

        public string GuildIdString => GuildId.ToString();

        [GuildProperty("Users and groups allowed to control bot and its guild properties")]
        public string ElevatedUsers { get; set; }

        [GuildProperty("Channels to which bot control is restricted")]
        public string ControlChannels { get; set; }

        [GuildProperty("Output channel for pings")]
        public string PingChannel { get; set; }

        [GuildProperty("Users and groups allowed to ping")]
        public string PingUsers { get; set; }

        [GuildProperty("Output channel for planned event reminders. If empty, ping channel is used")]
        public string RemindChannel { get; set; }

        [GuildProperty("Default time offsets for planned event reminders")]
        public string RemindOffsets { get; set; }

        public GuildProperties()
        {
            ElevatedUsers = string.Empty;
            ControlChannels = string.Empty;
            PingChannel = string.Empty;
            PingUsers = string.Empty;
            RemindChannel = string.Empty;
            RemindOffsets = string.Empty;
        }

        public string GetProperty(string name)
        {
            return GetPropertyInternal(name).GetValue(this) as string;
        }

        public void SetProperty(string name, string value)
        {
            GetPropertyInternal(name).SetValue(this, value);
        }

        public string[] GetPropertyNames()
        {
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => Attribute.IsDefined(p, typeof(GuildPropertyAttribute)))
                .Select(p => p.Name).ToArray();
        }

        public string GetPropertyDescription(string name)
        {
            return GuildPropertyAttribute.GetValue(GetPropertyInternal(name));
        }

        private PropertyInfo GetPropertyInternal(string name)
        {
            var prop = GetType().GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && Attribute.IsDefined(prop, typeof(GuildPropertyAttribute)))
                return prop;
            throw new ArgumentException(string.Format("Guild property '{0}' was not found", name));
        }
    }
}
