using Discord;
using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Pinger.Data;
using Schmellow.DiscordServices.Pinger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public sealed class MessagingService
    {
        private readonly ILogger<MessagingService> _logger;
        private readonly IGuildPropertyStorage _guildPropertyStorage;
        private readonly IDiscordClient _client;

        private bool _dmInProgress = false;

        public MessagingService(
            ILogger<MessagingService> logger,
            IGuildPropertyStorage guildPropertyStorage, 
            IDiscordClient client)
        {
            _logger = logger;
            _guildPropertyStorage = guildPropertyStorage;
            _client = client;
        }

        public async Task PingDefaultChannel(ulong guildId, string message)
        {
            var guildProperties = _guildPropertyStorage.EnsureGuildProperties(guildId);
            var channelName = guildProperties.PingChannel;
            if (string.IsNullOrEmpty(channelName))
                throw new Exception("Default ping channel is not set");
            await PingChannel(guildId, channelName, message);
        }

        public async Task PingChannel(ulong guildId, string channelName, string message)
        {
            IGuild guild = await _client.GetGuildAsync(guildId);
            if (guild == null)
                throw new Exception(string.Format("Guild {0} was not found", guildId));
            var channel = await guild.GetChannelByName<IMessageChannel>(channelName);
            if (channel == null)
                throw new Exception(string.Format("Channel {0} was not found on guild {1}", channelName, guild.Name));
            await PingChannel(channel, message);
        }

        public async Task PingChannel(IMessageChannel channel, string message)
        {
            if (!(channel is IGuildChannel))
                throw new Exception(string.Format("Channel {0} is not a guild channel", channel.Name));

            _logger.LogInformation("Pinging channel '{0}'", channel.Name);
            await channel.SendMessageAsync("@everyone\n" + message);
        }

        public async Task DMUser(ulong guildId, string userName, string message)
        {
            IGuild guild = await _client.GetGuildAsync(guildId);
            if (guild == null)
                throw new Exception(string.Format("Guild {0} was not found", guildId));
            var user = await guild.GetUserByName<IUser>(userName);
            if (user == null)
                throw new Exception(string.Format("User {0} was not found on guild {1}", userName, guild.Name));
            await DMUser(user, message);
        }

        public async Task DMUser(IUser user, string message)
        {
            if(!(user is IGuildUser))
                throw new Exception(string.Format("User {0} is not a guild user", user.Username + "#" + user.Discriminator));

            _logger.LogInformation("Sending message to " + user.Username + "#" + user.Discriminator);
            await user.SendMessageAsync(message);
        }

        public async Task MassDM(IMessageChannel feedbackChannel, IEnumerable<PrivateMessage> pms)
        {
            if (_dmInProgress)
                throw new Exception("Mass DM operation is in progress, please wait for it to finish");
            try
            {
                _dmInProgress = true;
                var span = TimeSpan.FromSeconds(pms.Count());
                await feedbackChannel.SendMessageAsync("Mass DM operation started. Estimated completion time: " + span);
                foreach (var pm in pms)
                {
                    DMUser(pm.User, pm.Message).GetAwaiter().GetResult();
                    Task.Delay(1000).GetAwaiter().GetResult();
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception("Unable to complete Mass DM operation");
            }
            finally
            {
                _dmInProgress = false;
                await feedbackChannel.SendMessageAsync("Mass DM finished");
            }
        }
    }
}
