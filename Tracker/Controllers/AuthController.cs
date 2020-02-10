using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schmellow.DiscordServices.Tracker.Data;
using Schmellow.DiscordServices.Tracker.Models;
using Schmellow.DiscordServices.Tracker.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Tracker.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly TrackerProperties _trackerProperties;
        private readonly ESIClient _esiClient;
        private readonly IUserStorage _userStorage;

        public AuthController(
            ILogger<AuthController> logger, 
            TrackerProperties trackerProperties,
            ESIClient esiClient, 
            IUserStorage userStorage)
        {
            _logger = logger;
            _trackerProperties = trackerProperties;
            _esiClient = esiClient;
            _userStorage = userStorage;
        }

        [Route("login")]
        public async Task<IActionResult> Login()
        {
            if (!string.IsNullOrEmpty(Request.QueryString.Value))
                return RedirectToAction("Login");

            if(_trackerProperties.AllowPublicHistoryAccess)
            {
                await PerformAuth("public");
                return RedirectToAction("Index", "History");
            }
            else
            {
                if (string.IsNullOrEmpty(_trackerProperties.EveClientId))
                {
                    _logger.LogError("Client ID is not configured");
                    return StatusCode(500);
                }
                if (string.IsNullOrEmpty(_trackerProperties.EveClientSecret))
                {
                    _logger.LogError("Client Secret is not configured");
                    return StatusCode(500);
                }

                var callback = Url.ActionLink("Callback", "Auth", null, Request.Scheme, Request.Host.Value);
                ViewData["AuthUrl"] = _esiClient.AuthUrl(
                    callback,
                    _trackerProperties.EveClientId);
                return View();
            }
        }

        [Route("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(_trackerProperties.EveClientId))
            {
                _logger.LogError("Client ID is not configured");
                return StatusCode(500);
            }
            if (string.IsNullOrEmpty(_trackerProperties.EveClientSecret))
            {
                _logger.LogError("Client Secret is not configured");
                return StatusCode(500);
            }

            var user = await _esiClient.GetUserFromSSO(
                code, 
                _trackerProperties.EveClientId, 
                _trackerProperties.EveClientSecret);

            if(user == null)
            {
                _logger.LogError("SSO Auth failed");
                return Unauthorized();
            }

            _logger.LogInformation(
                "Processing SSO Auth for character {0} - {1} - {2}",
                user.CharacterName,
                user.CorporationName,
                user.AllianceName);
            var storedUser = _userStorage.GetUser(user.CharacterName);
            if(storedUser == null || storedUser.AuthString != user.AuthString)
            {
                _logger.LogInformation("Character {0} was not whitelisted", user.CharacterName);
                return Unauthorized();
            }
            await PerformAuth(user.CharacterName);
            return RedirectToAction("Index", "History");
        }

        private async Task PerformAuth(string name)
        {
            _logger.LogInformation("Logging in '{0}'", name);
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, "user")
            };
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("Cookies", principal);
        }

        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Logging out '{0}'", HttpContext.User.Identity.Name);
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login");
        }
    }
}
