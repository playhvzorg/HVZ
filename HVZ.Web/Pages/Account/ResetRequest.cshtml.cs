using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Components;
using HVZ.Web.Identity.Models;
using HVZ.Web.Services;
using System.Web;
namespace HVZ.Web.Pages
{
    public class ResetRequestPageModel : PageModel
    {
        private UserManager<ApplicationUser> userManager;
        private IHttpContextAccessor httpContextAccessor;
        private ILogger<LoginModel> logger;
        private EmailService emailService;
        [BindProperty]
        public ResetRequestModel ResetRequestModelProperty { get; set; }
        public ResetRequestPageModel(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor, ILogger<LoginModel> logger, EmailService emailService)
        {
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            this.emailService = emailService;
            this.ResetRequestModelProperty = new();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();
            if (ResetRequestModelProperty == null)
                return Page();
            ApplicationUser? appUser = await userManager.FindByEmailAsync(ResetRequestModelProperty.Email);
            if (appUser == null)
                return Page();
            string passwordResetToken = await userManager.GeneratePasswordResetTokenAsync(appUser);
            await emailService.SendPasswordChangeEmailAsync(appUser.Email, appUser.FullName, HttpUtility.UrlEncode(passwordResetToken), appUser.Id.ToString());
            logger.LogInformation($"Password reset requested for {appUser.Email} from {HttpContext.Connection.RemoteIpAddress?.ToString()}");
            return Page();
        }
    }
}