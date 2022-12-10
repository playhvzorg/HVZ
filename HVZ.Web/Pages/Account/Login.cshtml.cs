using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HVZ.Web.Identity.Models;

namespace HVZ.Web.Pages
{
    public class LoginModel : PageModel
    {
        private UserManager<ApplicationUser> userManager;
        private SignInManager<ApplicationUser> signInManager;
        private string redirectUrl = "/";

        [BindProperty]
        public new SignInUserModel User { get; set; }

        public LoginModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.User = new();
        }

        public void OnGet(string returnUrl = "/")
        {
            this.redirectUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync()
        {         

            if(!ModelState.IsValid)
            {
                return Page();
            }

            if(User != null)
            {
                // Sign in and redirect to home
                ApplicationUser? authUser = await userManager.FindByEmailAsync(User.Email);
                if(authUser != null)
                {
                    Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(authUser, User.Password, User.RememberMe, false);
                    if(result.Succeeded)
                    {
                        return Redirect("/" + redirectUrl);
                    }
                    else
                    {
                        ModelState.AddModelError(nameof(User.Email), "Login Failed: Invalid Email or Password");
                    }
                }
                else
                {
                    System.Console.WriteLine("Wrong email");
                    ModelState.AddModelError(nameof(User.Email), "Login Failed: Invalid Email or Password");
                }
            }

            return Page();
        }
    }
}
