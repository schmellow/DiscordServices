using Microsoft.AspNetCore.Authentication.Cookies;
using Schmellow.DiscordServices.Tracker.Models;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Tracker.Services
{
    public static class CookieValidator
    {
        // Drop auth cookies on public-history mode change
        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var props = context?.HttpContext?.RequestServices?.GetService(typeof(TrackerProperties)) as TrackerProperties;
            if(props != null)
            {
                var userName = context?.Principal?.Identity?.Name;
                if(!string.IsNullOrEmpty(userName))
                {
                    if ((userName == "public" && props.AllowPublicHistoryAccess == false) ||
                        (userName != "public" && props.AllowPublicHistoryAccess == true))
                    {
                        context.RejectPrincipal();
                        return;
                    }
                }
            }
        }
    }
}
