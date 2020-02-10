using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schmellow.DiscordServices.Tracker.Data;
using Schmellow.DiscordServices.Tracker.Models;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Tracker.Services
{
    public sealed class TokenAuthenticationOptions : AuthenticationSchemeOptions
    {

    }


    public class TokenAuthenticationHandler : AuthenticationHandler<TokenAuthenticationOptions>
    {
        private const string HeaderName = "Authorization";
        private const string SchemeName = "Token";

        private readonly TrackerProperties _trackerProperties;
        private readonly IUserStorage _storage;

        public TokenAuthenticationHandler(
            IOptionsMonitor<TokenAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            TrackerProperties trackerProperties,
            IUserStorage storage)
            : base(options, logger, encoder, clock)
        {
            _trackerProperties = trackerProperties;
            _storage = storage;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(HeaderName))
            {
                Logger.LogError("Request does not contain auth header");
                return AuthenticateResult.Fail("Unauthorized");
            }
                
            AuthenticationHeaderValue headerValue;
            if (!AuthenticationHeaderValue.TryParse(Request.Headers[HeaderName], out headerValue))
            {
                Logger.LogError("Unable to parse auth header");
                return AuthenticateResult.Fail("Unauthorized");
            }
                
            if(headerValue.Scheme != SchemeName)
            {
                Logger.LogError("Wrong auth scheme: {0}", headerValue.Scheme);
                return AuthenticateResult.Fail("Unauthorized");
            }
            
            if(_trackerProperties.PingerToken != headerValue.Parameter)
            {
                Logger.LogError("Wrong pinger token - {0}", headerValue.Parameter);
                return AuthenticateResult.Fail("Unauthorized");
            }

            var claims = new[] { new Claim(ClaimTypes.Role, "bot") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }

    public static class TokenAuthenticationHandlerDefaults
    {
        public const string AuthenticationScheme = "Token";
    }

}
