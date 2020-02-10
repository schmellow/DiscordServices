namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class TrackerProperties
    {
        public string InstanceName { get; set; }
        public string DataDirectory { get; set; }
        public string PingerToken { get; set; }
        public bool AllowPublicHistoryAccess { get; set; }
        public string EveClientId { get; set; }
        public string EveClientSecret { get; set; }
        public string ProxyBasePath { get; set; }
        public string HttpPort { get; set; }
        public string HttpsPort { get; set; }
        public bool DisableHttp { get; set; }
        public bool DisableHttps { get; set; }
        public string UserQuery { get; set; }

        public TrackerProperties()
        {
            InstanceName = string.Empty;
            DataDirectory = string.Empty;
            PingerToken = string.Empty;
            AllowPublicHistoryAccess = false;
            EveClientId = string.Empty;
            EveClientSecret = string.Empty;
            ProxyBasePath = string.Empty;
            HttpPort = "5000";
            HttpsPort = "5001";
            DisableHttp = false;
            DisableHttps = false;
            UserQuery = string.Empty;
        }
    }
}
