using Discord;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Schmellow.DiscordServices.Pinger.Data;
using Schmellow.DiscordServices.Pinger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public sealed class TrackerService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly ILogger<TrackerService> _logger;
        private readonly BotProperties _botProperties;
        private readonly IGuildPropertyStorage _guildPropertyStorage;
        private readonly IDiscordClient _client;

        public TrackerService(
            ILogger<TrackerService> logger, 
            BotProperties botProperties, 
            IGuildPropertyStorage guildPropertyStorage,
            IDiscordClient client)
        {
            _logger = logger;
            _botProperties = botProperties;
            _guildPropertyStorage = guildPropertyStorage;
            _client = client;
        }

        public async Task<Dictionary<IUser, string>> GetTrackerUrlsDefault(
            ulong guildId,
            ulong authorId,
            string message)
        {
            var guildProperties = _guildPropertyStorage.EnsureGuildProperties(guildId);
            var channelName = guildProperties.PingChannel;
            if (string.IsNullOrEmpty(channelName))
                throw new Exception("Default ping channel is not set");
            return await GetTrackerUrls(guildId, authorId, channelName, message);
        }

        public async Task<Dictionary<IUser, string>> GetTrackerUrls(
            ulong guildId,
            ulong authorId,
            string channelName,
            string message)
        {
            IGuild guild = await _client.GetGuildAsync(guildId);
            if (guild == null)
                throw new Exception(string.Format("Guild {0} was not found", guildId));
            var author = await guild.GetUserAsync(authorId);
            if (author == null)
                throw new Exception(string.Format("User {0} was not found", authorId));
            var channel = await guild.GetChannelByName<IMessageChannel>(channelName);
            if (channel == null)
                throw new Exception(string.Format("Channel {0} was not found on guild {1}", channelName, guild.Name));
            return await GetTrackerUrls(author, channel, message);
        }

        public async Task<Dictionary<IUser, string>> GetTrackerUrls(
            IUser author,
            IChannel channel,
            string message)
        {
            var users = await channel.GetUsersAsync().FlattenAsync();
            return await GetTrackerUrls(
                author,
                users.Where(u => !u.IsBot && u.Id != author.Id),
                message);
        }

        public async Task<Dictionary<IUser, string>> GetTrackerUrls(
            IUser author,
            IEnumerable<IUser> users,
            string message)
        {
            if (string.IsNullOrEmpty(_botProperties.TrackerUrl))
                throw new ArgumentException("Tracker service URL is not set");
            if (string.IsNullOrEmpty(_botProperties.TrackerToken))
                throw new ArgumentException("Tracker service token is not set");
            try
            {
                Dictionary<string, IUser> userMap = users.ToDictionary(
                    u => u.Username + "#" + u.Discriminator,
                    u => u);
                var request = new PingRequest(userMap.Keys)
                {
                    Guild = ((IGuildUser)author).Guild.Name,
                    Author = author.Username + "#" + author.Discriminator,
                    Text = message
                };
                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    System.Text.Encoding.UTF8,
                    "application/json");
                using(var httpRequest = new HttpRequestMessage(HttpMethod.Post, _botProperties.TrackerUrl))
                {
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
                        "Token",
                        _botProperties.TrackerToken);
                    httpRequest.Content = content;
                    using(var response = await _httpClient.SendAsync(httpRequest))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception(string.Format(
                                "Tracker call unsuccessful - {0} ({1})", 
                                response.StatusCode, 
                                response.ReasonPhrase));
                        }
                        var jsonResponse = response.Content.ReadAsStringAsync().Result;
                        var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);
                        var baseUri = new Uri(_botProperties.TrackerUrl);
                        return result.ToDictionary(
                            kv => userMap[kv.Key],
                            kv => "<" + new Uri(baseUri, kv.Value).ToString() + ">");
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception("Unable to complete operation");
            }
        }

    }
}
