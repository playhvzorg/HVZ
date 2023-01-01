using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Components;
using HVZ.Web.Identity.Models;
using HVZ.Web.Services;
using System.Web;
namespace HVZ.Web.Pages
{
    public class ResetPageModel : PageModel
    {
        private UserManager<ApplicationUser> userManager;
        private IHttpContextAccessor httpContextAccessor;
        private ILogger<LoginModel> logger;
        private SignInManager<ApplicationUser> signInManager;
        [BindProperty]
        public ResetModel ResetModelProperty { get; set; }
        public ResetPageModel(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor, ILogger<LoginModel> logger, EmailService emailService, SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            this.signInManager = signInManager;
            this.ResetModelProperty = new();
        }

        public async Task<IActionResult> OnPostAsync(string requestId, string userId)
        {
            if (!ModelState.IsValid)
                return Page();
            if (ResetModelProperty == null)
                return Page();
            ApplicationUser? appUser = await userManager.FindByIdAsync(userId);
            if (appUser == null)
            {
                logger.LogDebug($"Could not find user with id: {userId}");
                return Page();
            }
            var result = await userManager.ResetPasswordAsync(appUser, requestId, ResetModelProperty.Password);
            if (result.Succeeded)
            {
                logger.LogInformation($"Password reset for {appUser.Email} from {HttpContext.Connection.RemoteIpAddress?.ToString()}");
                await signInManager.PasswordSignInAsync(appUser, ResetModelProperty.Password, false, false);
                return Redirect("/");
            }
            else
            {
                foreach (var error in result.Errors)
                    logger.LogError($"{error.Code}:{error.Description}");
            }
            return Page();
        }
    }
}