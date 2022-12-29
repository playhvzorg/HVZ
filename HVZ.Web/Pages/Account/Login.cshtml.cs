using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using HVZ.Web.Identity.Models;

namespace HVZ.Web.Pages
{
    public class LoginModel : PageModel
    {
        private UserManager<ApplicationUser> userManager;
        private SignInManager<ApplicationUser> signInManager;
        private NavigationManager navigation;
        private ILogger<LoginModel> logger;
        private HttpContextAccessor httpContextAccessor;

        [BindProperty]
        public SignInUserModel UserModel { get; set; }

        public LoginModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, NavigationManager navigation, ILogger<LoginModel> logger, HttpContextAccessor httpContextAccessor)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.UserModel = new();
            this.navigation = navigation;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = "/")
        {

            if (!ModelState.IsValid)
            {
                logger.LogDebug("invalid model state");
                return Page();
            }

            if (UserModel != null)
            {
                // Sign in and redirect to home
                ApplicationUser? authUser = await userManager.FindByEmailAsync(UserModel.Email);
                string? ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
                if (authUser != null)
                {
                    if (ip == null)
                    {
                        // Figure out what went wrong
                        if (httpContextAccessor.HttpContext == null)
                        {
                            logger.LogError($"HTTP Context is null\nUser Database ID: {authUser.DatabaseId}\nUser Identity ID: {authUser.Id}\nUser Email: {authUser.Email}");
                        }
                        else if (httpContextAccessor.HttpContext.Connection.RemoteIpAddress == null)
                        {
                            logger.LogError($"Remote connection is null\nUser Database ID: {authUser.DatabaseId}\nUser Identity ID: {authUser.Id}\nUser Email: {authUser.Email}");
                        }
                    }

                    Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(authUser, UserModel.Password, UserModel.RememberMe, false);
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"User: {{\n\tName: {authUser.FullName}\n\tEmail: {authUser.Email}\n\tDatabase ID: {authUser.DatabaseId}\n\tIdentity ID: {authUser.Id} \n}}\nLogin from: {ip ?? "Could not determine IP address"}");
                        await userManager.ResetAccessFailedCountAsync(authUser);
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        // Valid email, invalid password
                        await userManager.AccessFailedAsync(authUser);
                        int numFailedAttempts = await userManager.GetAccessFailedCountAsync(authUser);
                        if (numFailedAttempts > 1)
                        {
                            logger.LogWarning($"{numFailedAttempts} consecutive failed login attempts for email {authUser.Email}\nAttempt made from IP: {ip ?? "Could not determine IP address"}");
                        }
                    }
                }

                logger.LogDebug("Unsuccessful login attempt");
                ModelState.AddModelError(nameof(UserModel.Email), "Login Failed: Invalid Email or Password");
            }

            return Page();
        }
    }
}
