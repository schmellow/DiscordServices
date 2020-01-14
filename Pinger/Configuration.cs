using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger
{
    public sealed class GuildProperties
    {
        [StringSetProperty("Users allowed to control bot properties")]
        public HashSet<string> ElevatedUsers { get; set; }
        [StringSetProperty("Channels to which bot control is restricted")]
        public HashSet<string> ControlChannels { get; set; }
        [SingleStringProperty("Output channel for pings")]
        public string PingChannel { get; set; }
        [StringSetProperty("Users and groups allowed to ping")]
        public HashSet<string> PingUsers { get; set; }
        [IntegerProperty("Message delay. Zero means no delay")]
        public int MessageDelay { get; set; }
        [SingleStringProperty("Output channel for reminders. If empty, ping channel is used")]
        public string RemindChannel { get; set; }
        [StringSetProperty("Default time offsets for reminders")]
        public HashSet<string> RemindOffsets { get; set; }

        public GuildProperties()
        {
            ElevatedUsers = new HashSet<string>();
            ControlChannels = new HashSet<string>();
            PingChannel = string.Empty;
            PingUsers = new HashSet<string>();
            MessageDelay = 0;
            RemindChannel = string.Empty;
            RemindOffsets = new HashSet<string>();
        }
    }

    public sealed class Configuration
    {
        private readonly Dictionary<string, PropertyInfo> _properties = new Dictionary<string, PropertyInfo>();
        private readonly Dictionary<string, GuildPropertyAttribute> _attributes = new Dictionary<string, GuildPropertyAttribute>();

        [JsonIgnore]
        public string InstanceName { get; private set; }

        [JsonProperty(Order = 0)]
        public string Token { get; set; }

        [JsonProperty("GuildProperties", Order = 1)]
        private Dictionary<ulong, GuildProperties> _guildProperties = new Dictionary<ulong, GuildProperties>();

        [JsonIgnore]
        public IEnumerable<string> PropertyNames
        {
            get
            {
                return _properties.Values.Select(p => p.Name);
            }
        }

        public Configuration()
        {
            Token = string.Empty;
            _guildProperties = new Dictionary<ulong, GuildProperties>();
        }

        private void Init()
        {
            foreach(PropertyInfo property in typeof(GuildProperties).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = Attribute.GetCustomAttribute(property, typeof(GuildPropertyAttribute)) as GuildPropertyAttribute;
                if (attr != null)
                {
                    string name = property.Name.ToLowerInvariant();
                    _properties[name] = property;
                    _attributes[name] = attr;
                }
            }
        }

        public static Configuration Load(string instanceName)
        {
            Configuration configuration;
            var fileName = instanceName + ".json";
            if(File.Exists(fileName))
            {
                string json = File.ReadAllText(fileName);
                configuration = JsonConvert.DeserializeObject<Configuration>(json);
                configuration.InstanceName = instanceName;
            }
            else
            {
                configuration = new Configuration();
                configuration.InstanceName = instanceName;
                configuration.Save();
            }
            configuration.Init();
            return configuration;
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(InstanceName + ".json", json);
        }

        public string GetPropertyDescription(string propertyName)
        {
            GuildPropertyAttribute attr;
            if (!_attributes.TryGetValue(propertyName.ToLowerInvariant(), out attr))
                throw new ArgumentException(string.Format("Property '{0}' does not exist", propertyName));
            return attr.Description;
        }

        public string GetPropertyAsString(ulong guildId, string propertyName)
        {
            object value = GetProperty(guildId, propertyName);
            if (value == null)
                return string.Empty;
            if (value is IEnumerable && !(value is string))
                return string.Join(", ", ((IEnumerable)value).Cast<object>().Select(o => o.ToString()));
            return value.ToString();
        }

        public object GetProperty(ulong guildId, string propertyName)
        {
            PropertyInfo property;
            if (!_properties.TryGetValue(propertyName.ToLowerInvariant(), out property))
                throw new ArgumentException(string.Format("Property '{0}' does not exist", propertyName));
            GuildProperties guildProperties = GetGuildProperties(guildId);
            return property.GetValue(guildProperties);
        }

        public GuildProperties GetGuildProperties(ulong guildId)
        {
            GuildProperties props;
            if (_guildProperties.TryGetValue(guildId, out props))
                return props;
            return new GuildProperties();
        }

        public void SetProperty(ulong guildId, string propertyName, params string[] values)
        {
            string name = propertyName.ToLowerInvariant();

            PropertyInfo property;
            if (!_properties.TryGetValue(name, out property))
                throw new ArgumentException(string.Format("Property '{0}' does not exist", propertyName));

            GuildPropertyAttribute attr;
            if (!_attributes.TryGetValue(name, out attr))
                throw new ArgumentException(string.Format("Property '{0}' does not exist", propertyName));

            GuildProperties guildProperties;
            if (!_guildProperties.TryGetValue(guildId, out guildProperties))
            {
                guildProperties = new GuildProperties();
                _guildProperties[guildId] = guildProperties;
            }
                
            object value = attr.ParseValues(values);
            property.SetValue(guildProperties, value);
            Save();
        }
        
    }
}
