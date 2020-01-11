using System.Collections.Generic;

namespace Schmellow.DiscordServices.Pinger
{
    public static class BotProperties
    {
        public enum PropertyType
        {
            String,
            User,
            Users,
            Channel,
            Channels
        }

        public sealed class PropertyInfo
        {
            public readonly string Description;
            public readonly PropertyType Type;
            public PropertyInfo(string description, PropertyType type)
            {
                Description = description;
                Type = type;
            }
        }

        public const string TOKEN = "token";
        public const string ELEVATED_USERS = "elevated-users";
        public const string CONTROL_CHANNELS = "control-channels";
        public const string DEFAULT_PING_CHANNEL = "default-ping-channel";
        public const string PING_USERS = "ping-users";
        public const string PING_SPOOFING = "ping-spoofing";
        public const string SPOOFING_DELAY = "spoofing-delay";

        public static readonly Dictionary<string, PropertyInfo> ALL_PROPERTIES = new Dictionary<string, PropertyInfo>()
        {
            { TOKEN, new PropertyInfo("Auth token", PropertyType.String) },
            { ELEVATED_USERS, new PropertyInfo("Users allowed to control bot properties", PropertyType.Users) },
            { CONTROL_CHANNELS, new PropertyInfo("Channels to which bot control is restricted", PropertyType.Channels) },
            { DEFAULT_PING_CHANNEL, new PropertyInfo("Default output channel for pings", PropertyType.Channel) },
            { PING_USERS, new PropertyInfo("Users allowed to ping", PropertyType.Users) },
            { PING_SPOOFING, new PropertyInfo("Spoofing mode", PropertyType.String) },
            { SPOOFING_DELAY, new PropertyInfo("Spoofing delay in seconds", PropertyType.String) }
        };

        public static readonly HashSet<string> RESTRICTED_PROPERTIES = new HashSet<string>()
        {
            TOKEN
        };

        public static bool ExistsAndUnrestricted(string property)
        {
            return ALL_PROPERTIES.ContainsKey(property) && !RESTRICTED_PROPERTIES.Contains(property);
        }

        public static bool IsMulticolumn(string property)
        {
            if (!ALL_PROPERTIES.ContainsKey(property))
                return false;
            var type = ALL_PROPERTIES[property].Type;
            return type == PropertyType.Channels || type == PropertyType.Users;
        }
    }
}
