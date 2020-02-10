namespace Schmellow.DiscordServices.Tracker.Models
{
    public class User
    {
        public int LocalId { get; set; }

        public long CharacterId { get; set; }
        public long CorporationId { get; set; }
        public long AllianceId { get; set; }

        public string CharacterName { get; set; }
        public string CorporationName { get; set; }
        public string AllianceName { get; set; }

        public string AuthString
        {
            get
            {
                return AllianceId + "/" + CorporationId + "/" + CharacterId;
            }
        }
    }
}
