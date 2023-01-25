using System.Net.Http.Headers;
using System.Security.Claims;
using HVZ.Web.Identity.Models;
using HVZ.Web.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace HVZ.Web.Data
{
    [Route("discord/[action]")]
    public class DiscordAccountController : ControllerBase
    {
        private const string DISCORD_AUTH_URL = "https://discord.com/api/oauth2/token";
        public IDataProtectionProvider Provider { get; }

        private DiscordIntegrationSettings DiscordSettings;
        private UserManager<ApplicationUser> UserManager;
        private WebConfig WebConfig;

        public DiscordAccountController(IDataProtectionProvider provider, DiscordIntegrationSettings settings, UserManager<ApplicationUser> userManager, WebConfig webConfig)
        {
            Provider = provider;
            DiscordSettings = settings;
            UserManager = userManager;
            WebConfig = webConfig;
        }

        [HttpGet]
        public IActionResult Login()
        {
            string redirectUri = WebConfig.DomainName + "discord/logincallback";
            return Redirect($"https://discord.com/api/oauth2/authorize?client_id={DiscordSettings.ClientId!}&redirect_uri={redirectUri}&response_type=code&scope=identify");
        }

        public async Task<IActionResult> LoginCallback()
        {
            string accessToken = null!;
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, DISCORD_AUTH_URL);
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", DiscordSettings.ClientId! },
                { "client_secret", DiscordSettings.ClientSecret! },
                { "grant_type", "authorization_code" },
                { "code", Request.Query["code"]! },
                { "redirect_uri", WebConfig.DomainName + "discord/logincallback" },
                { "scope", "identify" },
            });
                var response = await client.SendAsync(request);
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                accessToken = json["access_token"].Value<string>();
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync("https://discord.com/api/users/@me");
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                string userId = json["id"].Value<string>();
                var user = await UserManager.GetUserAsync(HttpContext.User);
                await UserManager.RemoveClaimAsync(user, new Claim("DiscordId", string.Empty));
                await UserManager.AddClaimAsync(user, new Claim("DiscordId", userId));
            }
            return LocalRedirect("/");
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string returnUrl = "/")
        {
            await UserManager.RemoveClaimAsync(await UserManager.GetUserAsync(HttpContext.User), new Claim("DiscordId", string.Empty));
            return LocalRedirect(returnUrl);
        }
    }
}