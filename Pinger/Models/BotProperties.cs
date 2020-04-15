using System;
using System.Linq;

namespace Schmellow.DiscordServices.Pinger.Models
{
    public sealed class BotProperties
    {
        public string Command { get; set; }
        public string InstanceName { get; set; }
        public string DataDirectory { get; set; }
        public string DiscordToken { get; set; }
        public string TrackerUrl { get; set; }
        public string TrackerToken { get; set; }

        private BotProperties()
        {
            Command = string.Empty;
            InstanceName = string.Empty;
            DataDirectory = string.Empty;
            DiscordToken = string.Empty;
            TrackerUrl = string.Empty;
            TrackerToken = string.Empty;
        }

        private static readonly char[] _argSplitter = new char[] { '=' };
        public static BotProperties Parse(string[] args)
        {
            BotProperties botProperties = new BotProperties();
            botProperties.Command = args[0];
            var rest = args.Skip(1);
            botProperties.InstanceName = rest.FirstOrDefault(a => !a.StartsWith("--"));
            foreach (string arg in rest.Where(a => a.StartsWith("--")))
            {
                var tokens = arg.Split(_argSplitter, StringSplitOptions.RemoveEmptyEntries);
                string name = tokens[0].Trim('-');
                string value = "";
                if (tokens.Length > 1)
                    value = string.Join("=", tokens.Skip(1)).Trim('\'').Trim('"');
                switch (name)
                {
                    case "discord-token":
                        botProperties.DiscordToken = value;
                        break;
                    case "tracker-url":
                        botProperties.TrackerUrl = value;
                        break;
                    case "tracker-token":
                        botProperties.TrackerToken = value;
                        break;
                    case "data-directory":
                        botProperties.DataDirectory = value;
                        break;
                    default:
                        Console.WriteLine("Unknown parameter '{0}'", name);
                        break;
                }
            }
            return botProperties;
        }
    }
}
