using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using HVZ.Web.Identity.Models;
using System.Web;

namespace HVZ.Web.Pages
{
    public class VerifyModel : PageModel
    {
        private UserManager<ApplicationUser> userManager;

        public string Errors { get; set; } = "";
        public string VerificationState { get; set; } = "Verifying";
        public IEnumerable<IdentityError>? IdentityErrors { get; set; }

        public VerifyModel(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> OnGetAsync(string requestId)
        {
            System.Console.WriteLine("request ID: " + requestId);
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return Redirect($"Login?returnUrl=Verify?requestId={HttpUtility.UrlEncode(HttpUtility.UrlDecode(requestId))}");
            }

            string? email = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            ApplicationUser? appUser = await userManager.FindByEmailAsync(email ?? "");

            if (appUser == null)
            {
                // TODO: User logger LogFailure when available
                System.Console.WriteLine($"Could not find user matching email claim: {User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value}");
                VerificationState = "Error";
                return Page();
            }
            
            var result = await userManager.ConfirmEmailAsync(appUser, requestId.Replace(' ', '+'));

            if (result.Succeeded)
            {
                VerificationState = "Success";
                return Redirect("/profile/debug");
            }
            else
            {
                VerificationState = "Error";
                IdentityErrors = result.Errors;
                foreach (var error in result.Errors)
                {
                    // TODO: Use logger LogFailure when available
                    System.Console.WriteLine($"{error.Code}: {error.Description}");
                }
                return Page();
            }
        }
    }
}