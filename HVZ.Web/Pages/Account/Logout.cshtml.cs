using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HVZ.Web.Identity.Models;

namespace HVZ.Web.Pages
{
    [Authorize]
    public class LogoutModel : PageModel
    {
        private SignInManager<ApplicationUser> signInManager;
        private ILogger<LogoutModel> logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            this.signInManager = signInManager;
            this.logger = logger;
        }


        public async Task<IActionResult> OnGet()
        {
            await signInManager.SignOutAsync();
            logger.LogDebug("Successful Logout");
            return Redirect("/");
        }
    }
}