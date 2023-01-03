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
        private ILogger<VerifyModel> logger;

        public string Errors { get; set; } = "";
        public VerificationState VerificationState { get; set; } = VerificationState.VERIFYING;
        public IEnumerable<IdentityError>? IdentityErrors { get; set; }

        public VerifyModel(UserManager<ApplicationUser> userManager, ILogger<VerifyModel> logger)
        {
            this.userManager = userManager;
            this.logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string requestId)
        {
            logger.LogDebug($"request ID: {requestId}");
            if (User.Identity?.IsAuthenticated is not true)
            {
                return Redirect($"Login?returnUrl=Verify?requestId={HttpUtility.UrlEncode(HttpUtility.UrlDecode(requestId))}");
            }

            string? email = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            ApplicationUser? appUser = await userManager.FindByEmailAsync(email ?? "");

            if (appUser == null)
            {
                logger.LogError($"Could not find user matching email claim: {User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value}");
                VerificationState = VerificationState.ERROR;
                return Page();
            }

            var result = await userManager.ConfirmEmailAsync(appUser, requestId.Replace(' ', '+'));

            if (result.Succeeded)
            {
                VerificationState = VerificationState.SUCCESS;
                return Redirect("/profile/debug");
            }
            else
            {
                VerificationState = VerificationState.ERROR;
                IdentityErrors = result.Errors;
                foreach (var error in result.Errors)
                {
                    logger.LogError($"{error.Code}: {error.Description}");
                }
                return Page();
            }
        }
    }

    public enum VerificationState
    {
        VERIFYING,
        SUCCESS,
        ERROR
    }
}