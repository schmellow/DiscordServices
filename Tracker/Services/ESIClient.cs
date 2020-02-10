using Newtonsoft.Json.Linq;
using Schmellow.DiscordServices.Tracker.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Tracker.Services
{
    public sealed class ESIClient
    {
        private readonly HttpClient _client = new HttpClient();

        private static readonly string FIND_CHARACTER = "https://esi.evetech.net/latest/search/?categories=character&datasource=tranquility&language=en-us&search={0}&strict=true&user_agent=schmellow.discordservices.tracker";
        private static readonly string GET_CHARACTER = "https://esi.evetech.net/latest/characters/{0}/?user_agent=schmellow.discordservices.tracker";
        private static readonly string GET_ALLIANCE = "https://esi.evetech.net/latest/alliances/{0}/?user_agent=schmellow.discordservices.tracker";
        private static readonly string GET_CORP = "https://esi.evetech.net/latest/corporations/{0}/?user_agent=schmellow.discordservices.tracker";
        private static readonly string AUTH_TOKEN = "https://login.eveonline.com/oauth/token";
        private static readonly string AUTH_CHAR = "https://login.eveonline.com/oauth/verify";

        public string AuthUrl(string callback, string clientId)
        {
            callback = Uri.EscapeDataString(callback);
            return "https://login.eveonline.com/oauth/authorize/?response_type=code"
                + string.Format("&redirect_uri={0}", callback)
                + string.Format("&client_id={0}", clientId)
                + "&user_agent=schmellow.discordservices.tracker";
        }

        public async Task<User> GetUserByCharacterName(string name)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, string.Format(FIND_CHARACTER, name)))
            using (var response = await _client.SendAsync(request))
            {
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json) || json == "{}")
                    return null;
                JObject obj = JObject.Parse(json);
                long characterId;
                if (!long.TryParse(obj["character"].First.Value<string>(), out characterId))
                    return null;
                return await GetUserByCharacterId(characterId);
            }
        }

        public async Task<User> GetUserByCharacterId(long characterId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, string.Format(GET_CHARACTER, characterId)))
            using (var response = await _client.SendAsync(request))
            {
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json) || json == "{}")
                    return null;
                JObject obj = JObject.Parse(json);
                long allianceId, corpId;
                if (!long.TryParse(obj["alliance_id"].Value<string>(), out allianceId))
                    return null;
                if (!long.TryParse(obj["corporation_id"].Value<string>(), out corpId))
                    return null;
                string name = obj["name"].Value<string>();
                string allianceName = await GetAllianceName(allianceId);
                string corpName = await GetCorporationName(corpId);
                return new User()
                {
                    CharacterId = characterId,
                    CorporationId = corpId,
                    AllianceId = allianceId,
                    CharacterName = name,
                    CorporationName = corpName,
                    AllianceName = allianceName
                };
            }
        }

        private async Task<string> GetAllianceName(long allianceId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, string.Format(GET_ALLIANCE, allianceId)))
            using (var response = await _client.SendAsync(request))
            {
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json) || json == "{}")
                    return null;
                JObject obj = JObject.Parse(json);
                return obj["name"].Value<string>();
            }
        }

        private async Task<string> GetCorporationName(long corporationId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, string.Format(GET_CORP, corporationId)))
            using (var response = await _client.SendAsync(request))
            {
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json) || json == "{}")
                    return null;
                JObject obj = JObject.Parse(json);
                return obj["name"].Value<string>();
            }
        }

        public async Task<User> GetUserFromSSO(string code, string id, string secret)
        {
            var token = await GetToken(code, id, secret);
            if (string.IsNullOrEmpty(token))
                return null;
            using (var request = new HttpRequestMessage(HttpMethod.Get, AUTH_CHAR))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                using (var response = await _client.SendAsync(request))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(json) || json == "{}")
                        return null;
                    JObject obj = JObject.Parse(json);
                    long characterId = 0;
                    if (!long.TryParse(obj["CharacterID"].Value<string>(), out characterId))
                        return null;
                    return await GetUserByCharacterId(characterId);
                }
            }
        }

        public async Task<string> GetToken(string code, string id, string secret)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, AUTH_TOKEN))
            {
                string auth = string.Format("{0}:{1}", id, secret);
                byte[] authBytes = Encoding.UTF8.GetBytes(auth);
                string authBase64 = Convert.ToBase64String(authBytes);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authBase64);
                request.Content = new StringContent(
                    string.Format("{{'grant_type':'authorization_code','code':'{0}'}}", code),
                    System.Text.Encoding.UTF8,
                    "application/json");
                using (var response = await _client.SendAsync(request))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(json) || json == "{}")
                        return string.Empty;
                    JObject obj = JObject.Parse(json);
                    return obj["access_token"].Value<string>();
                }
            }
        }

    }
}
