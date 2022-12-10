using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HVZ.Web.Identity.Models;

namespace HVZ.Web.Pages
{
    public class LogoutModel : PageModel
    {
        private SignInManager<ApplicationUser> signInManager;

        public LogoutModel(SignInManager<ApplicationUser> signInManager)
        {
            this.signInManager = signInManager;
        }

        [Authorize]
        public async Task<IActionResult> OnGet()
        {
            await signInManager.SignOutAsync();
            return Redirect("/");
        }
    }
}