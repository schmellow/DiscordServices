namespace Schmellow.DiscordServices.Pinger.Models
{
    public sealed class BotProperties
    {
        public string InstanceName { get; set; }
        public string DataDirectory { get; set; }
        public string DiscordToken { get; set; }
        public string TrackerUrl { get; set; }
        public string TrackerToken { get; set; }

        public BotProperties()
        {
            InstanceName = string.Empty;
            DataDirectory = string.Empty;
            DiscordToken = string.Empty;
            TrackerUrl = string.Empty;
            TrackerToken = string.Empty;
        }
    }
}
