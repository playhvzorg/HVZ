using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace HVZ.Web.Data
{
    [Route("discord/[action]")]
    public class DiscordAccountController : ControllerBase
    {
        public IDataProtectionProvider Provider { get; }

        public DiscordAccountController(IDataProtectionProvider provider)
        {
            Provider = provider;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties {RedirectUri = returnUrl}, "Discord");
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string returnUrl = "/")
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return LocalRedirect(returnUrl);
        }
    }
}