using System.Net.Http.Headers;
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
        private SignInManager<ApplicationUser> SignInManager;

        public DiscordAccountController(IDataProtectionProvider provider, DiscordIntegrationSettings settings, UserManager<ApplicationUser> userManager, WebConfig webConfig, SignInManager<ApplicationUser> signInManager)
        {
            Provider = provider;
            DiscordSettings = settings;
            UserManager = userManager;
            WebConfig = webConfig;
            SignInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (DiscordSettings.ClientId is null || DiscordSettings.ClientSecret is null)
                return LocalRedirect("/");
            string redirectUri = WebConfig.DomainName + "discord/logincallback";
            return Redirect($"https://discord.com/api/oauth2/authorize?client_id={DiscordSettings.ClientId!}&redirect_uri={redirectUri}&response_type=code&scope=identify");
        }

        public async Task<IActionResult> LoginCallback()
        {
            if (DiscordSettings.ClientId is null || DiscordSettings.ClientSecret is null)
                return LocalRedirect("/");
            string accessToken = null!;
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, DISCORD_AUTH_URL);
                string? code = Request.Query["code"];
                if (code is null)
                    throw new InvalidOperationException("Error getting code property. Make sure you are starting at discord/login and discord will provide the code");
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", DiscordSettings.ClientId },
                { "client_secret", DiscordSettings.ClientSecret },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", WebConfig.DomainName + "discord/logincallback" },
                { "scope", "identify" },
            });
                var response = await client.SendAsync(request);
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                accessToken = json["access_token"]?.Value<string>()!;
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync("https://discord.com/api/users/@me");
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                string userId = json["id"]?.Value<string>()!;
                var user = await UserManager.GetUserAsync(HttpContext.User);
                if (user is null)
                    throw new InvalidOperationException("Must be signed in with an account to link discord");
                user.DiscordId = userId;
                await UserManager.UpdateAsync(user);
                await SignInManager.RefreshSignInAsync(user);

            }
            return LocalRedirect("/");
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string returnUrl = "/")
        {
            var user = await UserManager.GetUserAsync(HttpContext.User);
            if (user is null)
                return LocalRedirect(returnUrl);
            user.DiscordId = string.Empty;
            await UserManager.UpdateAsync(user);
            await SignInManager.RefreshSignInAsync(user);
            return LocalRedirect(returnUrl);
        }
    }
}