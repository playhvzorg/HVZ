using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Components;
using HVZ.Web.Identity.Models;

namespace HVZ.Web.Pages
{
    public class LoginModel : PageModel
    {
        private UserManager<ApplicationUser> userManager;
        private SignInManager<ApplicationUser> signInManager;
        private NavigationManager navigation;
        private ILogger<LoginModel> _logger;
        private string redirectUrl = "";

        [BindProperty]
        public SignInUserModel UserModel { get; set; }

        public LoginModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, NavigationManager navigation, ILogger<LoginModel> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.UserModel = new();
            this.navigation = navigation;
            this._logger = logger;
        }

        public void OnGet(string returnUrl = "/")
        {
            this.redirectUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = "/")
        {

            if (!ModelState.IsValid)
            {
                _logger.LogDebug("invalid model state");
                return Page();
            }

            if (UserModel != null)
            {
                // Sign in and redirect to home
                ApplicationUser? authUser = await userManager.FindByEmailAsync(UserModel.Email);
                if (authUser != null)
                {
                    Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(authUser, UserModel.Password, UserModel.RememberMe, false);
                    if (result.Succeeded)
                    {
                        _logger.LogDebug("Successful login");
                        return Redirect(returnUrl);
                    }
                }
                _logger.LogDebug("Unsuccessful login attempt");
                ModelState.AddModelError(nameof(UserModel.Email), "Login Failed: Invalid Email or Password");
            }

            return Page();
        }
    }
}
