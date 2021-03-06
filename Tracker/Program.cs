using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using Schmellow.DiscordServices.Tracker.Data;
using Schmellow.DiscordServices.Tracker.Models;
using Schmellow.DiscordServices.Tracker.Services;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Tracker
{
    public class Program
    {
        private readonly static string PIPE_NAME = "Schmellow.DiscordServices.Tracker.";

        private static NLog.Logger _logger = null;
        private static string _instanceName = string.Empty;
        private static Task _hostStopperTask = null;

        public static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0 || args[0] == "help")
                {
                    Console.WriteLine("Expecting command");
                    Console.WriteLine("Available commands:");
                    Console.WriteLine(" * run <instanceName> <arguments>");
                    Console.WriteLine("   --pinger-token - pinger client token [string, MANDATORY]");
                    Console.WriteLine("   --eve-id - EVE application client id [string, default=none]");
                    Console.WriteLine("   --eve-secret - EVE application client secret [string, default=none]");
                    Console.WriteLine("   --data-directory - data storage location [string, default=./]");
                    Console.WriteLine("   --proxy-basepath - proxy subdirectory base path [string, default=none]");
                    Console.WriteLine("   --http-port - port to listen for HTTP requests [string, default=5000]");
                    Console.WriteLine("   --https-port - port to listen for HTTPS requests [string, default=5001]");
                    Console.WriteLine("   --disable-http - disable plain HTTP [switch, default unset]");
                    Console.WriteLine("   --disable-https - disable HTTPS [switch, default unset]");
                    Console.WriteLine("   --public-history - allow public history access [switch, default unset]");
                    Console.WriteLine(" * stop <instanceName>");
                    Console.WriteLine(" * add-user <instanceName> <arguments>");
                    Console.WriteLine("   --character - EVE character name [string, MANDATORY]");
                    Console.WriteLine("   --servers - allow browsing history only for specific servers [string, comma/semicolon separated, default=none]");
                    Console.WriteLine(" * remove-user <instanceName> <arguments>");
                    Console.WriteLine("   --character - EVE character name [string, MANDATORY]");
                    Console.WriteLine(" * list-users <instanceName>");
                    Console.WriteLine(" * generate-token");
                    return;
                }

                TrackerProperties trackerProperties = TrackerProperties.Parse(args);
                _instanceName = trackerProperties.InstanceName;

                NLog.LayoutRenderers.LayoutRenderer.Register("instance", (logevent) => _instanceName);
                _logger = NLog.Web.NLogBuilder.ConfigureNLog("logger.config").GetCurrentClassLogger();

                if (trackerProperties.Command == "generate-token")
                {
                    GenerateToken();
                }
                else
                {
                    if (string.IsNullOrEmpty(_instanceName))
                        throw new ArgumentException("Instance name is not set");

                    if (trackerProperties.Command == "run")
                    {
                        Run(trackerProperties);
                    }
                    else if (trackerProperties.Command == "stop")
                    {
                        Stop();
                    }
                    else if (trackerProperties.Command == "add-user")
                    {
                        AddUser(trackerProperties);
                    }
                    else if (trackerProperties.Command == "remove-user")
                    {
                        RemoveUser(trackerProperties);
                    }
                    else if (trackerProperties.Command == "list-users")
                    {
                        ListUsers(trackerProperties);
                    }
                    else
                    {
                        throw new ArgumentException("Unknown command '" + trackerProperties.Command + "'");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        // courtesy of https://dotnetfiddle.net/grgEIh
        private static void GenerateToken()
        {
            string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            int length = 64;

            const int byteSize = 0x100;
            var allowedCharSet = new HashSet<char>(allowedChars).ToArray();

            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                var result = new StringBuilder();
                var buf = new byte[128];
                while (result.Length < length)
                {
                    rng.GetBytes(buf);
                    for (var i = 0; i < buf.Length && result.Length < length; ++i)
                    {
                        var outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
                        if (outOfRangeStart <= buf[i]) continue;
                        result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
                    }
                }
                Console.WriteLine(result.ToString());
            }
        }

        private static void Run(TrackerProperties trackerProperties)
        {
            if (IsRunning())
                throw new Exception("Instance '" + _instanceName + "' is already running");

            LogInfo("Running instance '{0}'", _instanceName);

            if (string.IsNullOrEmpty(trackerProperties.PingerToken))
                throw new ArgumentException("Pinger token is not set");

            if (!trackerProperties.AllowPublicHistoryAccess &&
                (string.IsNullOrEmpty(trackerProperties.EveClientId) ||
                string.IsNullOrEmpty(trackerProperties.EveClientSecret)))
            {
                LogWarning("Eve client parameters are not configured properly, history pages will be inaccessible");
            }

            if(trackerProperties.DisableHttp)
            {
                LogWarning("HTTP is disabled");
            }

            if(trackerProperties.DisableHttps)
            {
                LogWarning("HTTPS is disabled");
            }

            if (trackerProperties.DisableHttp && trackerProperties.DisableHttps)
                throw new ArgumentException("Can't disable both HTTP and HTTPS");

            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(trackerProperties);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    if (trackerProperties.DisableHttp)
                    {
                        webBuilder.UseUrls(
                            string.Format("https://localhost:{0}", trackerProperties.HttpsPort));
                    }
                    else if(trackerProperties.DisableHttps)
                    {
                        webBuilder.UseUrls(
                            string.Format("http://localhost:{0}", trackerProperties.HttpPort));
                    }
                    else
                    {
                        webBuilder.UseUrls(
                            string.Format("http://localhost:{0}", trackerProperties.HttpPort),
                            string.Format("https://localhost:{0}", trackerProperties.HttpsPort));
                    }
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                })
                .UseNLog()
                .Build();
            _hostStopperTask = Task.Run(() => SetupHostStopper(host));
            host.Run();
        }

        private static void Stop()
        {
            if (IsRunning())
                throw new Exception("Instance '" + _instanceName + "' is not running");

            LogInfo("Stopping instance '{0}'", _instanceName);

            try
            {
                using (var client = new NamedPipeClientStream(".", PIPE_NAME + _instanceName, PipeDirection.Out))
                {
                    client.Connect(1000);
                    client.WriteByte(2);
                }
            }
            catch (TimeoutException)
            {
                // Suppress timeout exception
            }
        }

        private static void SetupHostStopper(IHost host)
        {
            bool run = true;
            while (run)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PIPE_NAME + _instanceName, PipeDirection.InOut))
                    {
                        server.WaitForConnection();
                        switch (server.ReadByte())
                        {
                            case 1:
                                server.WriteByte(1);
                                break;
                            case 2:
                                run = false;
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex.ToString());
                    run = false;
                }
            }
            LogInfo("Stop command received, shutting down host");
            host.StopAsync().GetAwaiter().GetResult();
        }

        private static bool IsRunning()
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PIPE_NAME + _instanceName, PipeDirection.InOut))
                {
                    client.Connect(1000);
                    client.WriteByte(1);
                    return client.ReadByte() == 1;
                }
            }
            catch (TimeoutException)
            {
                // Suppress timeout exception
            }
            return false;
        }

        private static void AddUser(TrackerProperties trackerProperties)
        {
            if (string.IsNullOrEmpty(trackerProperties.CharacterName))
                throw new ArgumentException("Character name is not set");
            var client = new ESIClient();
            var user = client.GetUserByCharacterName(trackerProperties.CharacterName).GetAwaiter().GetResult();
            if (user == null)
            {
                Console.WriteLine("ERROR: unable to load EVE character {0}", trackerProperties.CharacterName);
                return;
            }
            if(!string.IsNullOrEmpty(trackerProperties.ServerRestrictions))
            {
                var serverTokens = trackerProperties.ServerRestrictions
                    .Split(
                        new char[] { ';', ',' }, 
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim());
                user.RestrictedServers.Clear();
                foreach (string token in serverTokens)
                    user.RestrictedServers.Add(token);
            }
            using (var storage = new LiteDBStorage(trackerProperties))
            {
                var storedUser = storage.GetUser(user.CharacterName);
                if(storedUser != null)
                {
                    user.LocalId = storedUser.LocalId;
                    if (storage.UpdateUser(user))
                    {
                        Console.WriteLine(
                            "Updated user [{0}][{1}] {2} - [{3}] {4} - [{5}] {6}",
                            user.LocalId,
                            user.CharacterId,
                            user.CharacterName,
                            user.CorporationId,
                            user.CorporationName,
                            user.AllianceId,
                            user.AllianceName);
                        if(user.RestrictedServers.Any())
                        {
                            Console.WriteLine("Server restrictions:");
                            foreach(var s in user.RestrictedServers)
                                Console.WriteLine(" - {0}", s);
                        }
                        else
                        {
                            Console.WriteLine("No server restrictions set");
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: user update failed");
                    }
                }
                else
                {
                    var id = storage.AddUser(user);
                    if (id > 0)
                    {
                        Console.WriteLine(
                            "Added user [{0}][{1}] {2} - [{3}] {4} - [{5}] {6}",
                            id,
                            user.CharacterId,
                            user.CharacterName,
                            user.CorporationId,
                            user.CorporationName,
                            user.AllianceId,
                            user.AllianceName);
                        if (user.RestrictedServers.Any())
                        {
                            Console.WriteLine("Server restrictions:");
                            foreach (var s in user.RestrictedServers)
                                Console.WriteLine(" - {0}", s);
                        }
                        else
                        {
                            Console.WriteLine("No server restrictions set");
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: user creation failed");
                    }
                }
            }
        }

        private static void RemoveUser(TrackerProperties trackerProperties)
        {
            if (string.IsNullOrEmpty(trackerProperties.CharacterName))
                throw new ArgumentException("Character name is not set");
            using (var storage = new Data.LiteDBStorage(trackerProperties))
            {
                if (storage.DeleteUser(trackerProperties.CharacterName))
                {
                    Console.WriteLine("Deleted user {0}", trackerProperties.CharacterName);
                }
                else
                {
                    Console.WriteLine("Error: user {0} was not found", trackerProperties.CharacterName);
                }
            }
        }

        private static void ListUsers(TrackerProperties trackerProperties)
        {
            using (var storage = new LiteDBStorage(trackerProperties))
            {
                foreach (User user in storage.GetUsers())
                {
                    Console.WriteLine(
                        "[{0}][{1}] {2} - [{3}] {4} - [{5}] {6}",
                        user.LocalId,
                        user.CharacterId,
                        user.CharacterName,
                        user.CorporationId,
                        user.CorporationName,
                        user.AllianceId,
                        user.AllianceName);
                    if(user.RestrictedServers.Any())
                    {
                        foreach(var s in user.RestrictedServers)
                            Console.WriteLine(" - {0}", s);
                    }
                }
            }
        }

        private static void LogInfo(string message, params string[] args)
        {
            message = string.Format(message, args);
            if (_logger != null)
                _logger.Info(message);
            else
                Console.WriteLine(message);
        }

        private static void LogWarning(string message, params string[] args)
        {
            message = string.Format(message, args);
            if (_logger != null)
                _logger.Warn(message);
            else
                Console.WriteLine("WARNING: " + message);
        }

        private static void LogError(string message, params string[] args)
        {
            message = string.Format(message, args);
            if (_logger != null)
                _logger.Error(message);
            else
                Console.WriteLine("ERROR: " + message);
        }

    }
}
