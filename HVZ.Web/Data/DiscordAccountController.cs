namespace HVZ.Web.Data;

using System.Net.Http.Headers;
using Identity.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Settings;

[Route("discord/[action]")]
public class DiscordAccountController : ControllerBase {
    private const string DiscordAuthUrl = "https://discord.com/api/oauth2/token";

    private readonly DiscordIntegrationSettings DiscordSettings;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> UserManager;
    private readonly WebConfig webConfig;

    public DiscordAccountController(IDataProtectionProvider provider, DiscordIntegrationSettings settings, UserManager<ApplicationUser> userManager, WebConfig webConfig, SignInManager<ApplicationUser> signInManager)
    {
        Provider = provider;
        DiscordSettings = settings;
        UserManager = userManager;
        this.webConfig = webConfig;
        this.signInManager = signInManager;
    }

    public IDataProtectionProvider Provider { get; }

    [HttpGet]
    public IActionResult Login()
    {
        if (DiscordSettings.ClientId is null || DiscordSettings.ClientSecret is null)
            return LocalRedirect("/");
        string redirectUri = webConfig.DomainName + "discord/logincallback";
        return Redirect($"https://discord.com/api/oauth2/authorize?client_id={DiscordSettings.ClientId!}&redirect_uri={redirectUri}&response_type=code&scope=identify");
    }

    public async Task<IActionResult> LoginCallback()
    {
        if (DiscordSettings.ClientId is null || DiscordSettings.ClientSecret is null)
            return LocalRedirect("/");
        string accessToken = null!;
        using (var client = new HttpClient())
        {
            var request = new HttpRequestMessage(HttpMethod.Post, DiscordAuthUrl);
            string? code = Request.Query["code"];
            if (code is null)
                throw new InvalidOperationException("Error getting code property. Make sure you are starting at discord/login and discord will provide the code");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", DiscordSettings.ClientId },
                { "client_secret", DiscordSettings.ClientSecret },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", webConfig.DomainName + "discord/logincallback" },
                { "scope", "identify" }
            });
            HttpResponseMessage response = await client.SendAsync(request);
            JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
            accessToken = json["access_token"]?.Value<string>()!;
        }
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await client.GetAsync("https://discord.com/api/users/@me");
            JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
            string userId = json["id"]?.Value<string>()!;
            ApplicationUser? user = await UserManager.GetUserAsync(HttpContext.User);
            if (user is null)
                throw new InvalidOperationException("Must be signed in with an account to link discord");
            user.DiscordId = userId;
            await UserManager.UpdateAsync(user);
            await signInManager.RefreshSignInAsync(user);

        }
        return LocalRedirect("/");
    }

    [HttpGet]
    public async Task<IActionResult> Logout(string returnUrl = "/")
    {
        ApplicationUser? user = await UserManager.GetUserAsync(HttpContext.User);
        if (user is null)
            return LocalRedirect(returnUrl);
        user.DiscordId = string.Empty;
        await UserManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);
        return LocalRedirect(returnUrl);
    }
}