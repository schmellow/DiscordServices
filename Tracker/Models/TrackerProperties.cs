using System;
using System.Linq;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public sealed class TrackerProperties
    {
        public string Command { get; set; }
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
        public string CharacterName { get; set; }
        public string ServerRestrictions { get; set; }

        private TrackerProperties()
        {
            Command = string.Empty;
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
            CharacterName = string.Empty;
            ServerRestrictions = string.Empty;
        }

        private static readonly char[] _argSplitter = new char[] { '=' };
        public static TrackerProperties Parse(string[] args)
        {
            TrackerProperties trackerProperties = new TrackerProperties();
            trackerProperties.Command = args[0];
            var rest = args.Skip(1);
            trackerProperties.InstanceName = rest.FirstOrDefault(a => !a.StartsWith("--"));
            foreach (string arg in rest.Where(a => a.StartsWith("--")))
            {
                var tokens = arg.Split(_argSplitter, StringSplitOptions.RemoveEmptyEntries);
                string name = tokens[0].Trim('-');
                string value = "";
                if (tokens.Length > 1)
                    value = string.Join("=", tokens.Skip(1)).Trim('\'').Trim('"');
                switch (name)
                {
                    case "pinger-token":
                        trackerProperties.PingerToken = value;
                        break;
                    case "eve-id":
                        trackerProperties.EveClientId = value;
                        break;
                    case "eve-secret":
                        trackerProperties.EveClientSecret = value;
                        break;
                    case "data-directory":
                        trackerProperties.DataDirectory = value;
                        break;
                    case "proxy-basepath":
                        trackerProperties.ProxyBasePath = value.StartsWith('/') ? value : "/" + value;
                        break;
                    case "http-port":
                        int httpPort;
                        if (int.TryParse(value, out httpPort))
                            trackerProperties.HttpPort = httpPort.ToString();
                        else
                            Console.WriteLine("Unable to parse HTTP port value");
                        break;
                    case "https-port":
                        int httpsPort;
                        if (int.TryParse(value, out httpsPort))
                            trackerProperties.HttpsPort = httpsPort.ToString();
                        else
                            Console.WriteLine("Unable to parse HTTPS port value");
                        break;
                    case "disable-http":
                        trackerProperties.DisableHttp = true;
                        break;
                    case "disable-https":
                        trackerProperties.DisableHttps = true;
                        break;
                    case "public-history":
                        trackerProperties.AllowPublicHistoryAccess = true;
                        break;
                    case "character":
                        trackerProperties.CharacterName = value;
                        break;
                    case "servers":
                        trackerProperties.ServerRestrictions = value;
                        break;
                    default:
                        Console.WriteLine("Unknown parameter '{0}'", name);
                        break;
                }
            }
            return trackerProperties;
        }
    }
}
