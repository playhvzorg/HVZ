using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HVZ.Web.Identity.Models;

namespace HVZ.Web.Pages
{
    public class VerifyModel : PageModel
    {
        private UserManager<ApplicationUser> userManager;

        public VerifyModel(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(string requestId)
        {
            // TODO: Check that the user is signed in
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return Redirect($"Account/Login?redirectURL=Account/Verify?requestId={requestId}");
            }

            ApplicationUser? appUser = await userManager.FindByEmailAsync(User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value);

            if (appUser == null)
            {
                // TODO: Something has gone wrong
                return Page();
            }

            var result = await userManager.ConfirmEmailAsync(appUser, requestId);

            if (result.Succeeded)
            {
                System.Console.WriteLine("success");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    System.Console.WriteLine($"{error.Code}: {error.Description}");
                }
            }

            return Redirect("/");
        }
    }
}