using System.Net.Http.Headers;
using HVZ.Web.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
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

        public DiscordAccountController(IDataProtectionProvider provider, DiscordIntegrationSettings settings)
        {
            Provider = provider;
            DiscordSettings = settings;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/discord/logincallback")
        {
            string redirectUri = "https://localhost:7284/discord/logincallback";
            return Redirect($"https://discord.com/api/oauth2/authorize?client_id={DiscordSettings.ClientId!}&redirect_uri={redirectUri}&response_type=code&scope=identify");
        }

        public async Task<IActionResult> LoginCallback(string returnUrl = "https://localhost:7284/discord/logincallback")
        {
            string accessToken = null!;
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, DISCORD_AUTH_URL);
                string code = Request.Query["code"]!;
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", DiscordSettings.ClientId! },
                { "client_secret", DiscordSettings.ClientSecret! },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", returnUrl },
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
                //TODO: put this in user's claims as 'DiscordId'
                Console.WriteLine($"DiscordId: {userId}");
            }
            return LocalRedirect("/");
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string returnUrl = "/")
        {
            //TODO: delete discordid from the user's claims
            return LocalRedirect(returnUrl);
        }
    }
}