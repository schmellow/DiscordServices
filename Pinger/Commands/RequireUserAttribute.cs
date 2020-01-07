﻿using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Pinger.Commands
{
    /// <summary>
    /// Checks that command is executed in by user whitelisted through specific UserProperty
    /// </summary>
    public sealed class RequireUserAttribute : PreconditionAttribute
    {
        string _errorMessage;
        string _userProperty;

        public RequireUserAttribute(string userProperty)
        {
            _userProperty = userProperty;
        }

        public override string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, 
            CommandInfo command, 
            IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                var storage = (IStorage)services.GetService(typeof(IStorage));
                var users = storage.GetProperty(_userProperty);
                if(users.Contains(gUser.Username))
                    return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError(_errorMessage));
        }
    }
}