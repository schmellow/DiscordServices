using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Services
{
    public sealed class MessagingService
    {
        private ILogger _logger;
        private Configuration _configuration;
        private IDiscordClient _client;

        public MessagingService(ILogger logger, Configuration configuration, IDiscordClient client)
        {
            _logger = logger;
            _configuration = configuration;
            _client = client;
        }

        public async Task PingDefaultChannel(ulong guildId, string message, Embed embed)
        {
            var guildProperties = _configuration.GetGuildProperties(guildId);
            if (string.IsNullOrEmpty(guildProperties.PingChannel))
                throw new Exception("Default ping channel is not set");
            await PingChannel(guildId, guildProperties.PingChannel, message, embed);
        }

        public async Task PingChannel(ulong guildId, string channelName, string message, Embed embed)
        {
            _logger.Info("Pinging channel '{0}'", channelName);
            IGuild guild = await _client.GetGuildAsync(guildId);
            if (guild == null)
                throw new Exception(string.Format("Guild {0} was not found", guildId));

            var channels = await guild.GetChannelsAsync();
            IMessageChannel channel = channels
                .Where(c => c is IMessageChannel && c.Name == channelName)
                .FirstOrDefault() as IMessageChannel;
            if (channel == null)
                throw new Exception(string.Format("Channel {0} was not found", channelName));
            await PingChannel(channel, message, embed);
        }

        public async Task PingChannel(IMessageChannel channel, string message, Embed embed)
        {
            if(channel is IGuildChannel gChannel)
            {
                var guildProperties = _configuration.GetGuildProperties(gChannel.GuildId);
                if(guildProperties.MessageDelay > 0)
                {
                    var sentMessage = await channel.SendMessageAsync("@everyone\n" + message);
                    await Task.Delay(guildProperties.MessageDelay * 1000);
                    await sentMessage.ModifyAsync(m => m.Embed = embed);
                }
                else
                {
                    await channel.SendMessageAsync("@everyone\n" + message, false, embed);
                }
            }
            else
            {
                throw new Exception(string.Format("Channel {0} is not a guild channel", channel.Name));
            }            
        }

        public async Task PingEvent(DateTime pingDate, ulong guildId, ScheduledEvent se)
        {
            try
            {
                GuildProperties properties = _configuration.GetGuildProperties(guildId);
                string channelName;
                if (se.TargetDate > pingDate)
                {
                    _logger.Info("Pinging reminder for event {0}/[{1}]", guildId, se.Id);
                    channelName = properties.RemindChannel;
                    if (string.IsNullOrEmpty(channelName))
                        channelName = properties.PingChannel;
                    if (string.IsNullOrEmpty(channelName))
                        throw new Exception("Neither remind nor ping channels are set for guild " + guildId);
                }
                else
                {
                    _logger.Info("Pinging main event {0}/[{1}]", guildId, se.Id);
                    channelName = properties.PingChannel;
                    if (string.IsNullOrEmpty(channelName))
                        throw new Exception("Default ping channel is not set for guild " + guildId);
                }
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.Title = se.ETA;
                embedBuilder.Description = se.Message;
                embedBuilder.AddField("From " + se.User, "\u200b");
                await PingChannel(guildId, channelName, string.Empty, embedBuilder.Build());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
            }
        }

    }
}
