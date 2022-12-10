using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HVZ.Web.Identity.Models;
using HVZ.Models;
using HVZ.Persistence;

namespace HVZ.Web.Pages
{
    public class RegisterModel : PageModel
    {

        private UserManager<ApplicationUser> userManager;
        private SignInManager<ApplicationUser> signInManager;
        private IUserRepo userRepo;
        private string redirectURL = "/";

        [BindProperty]
        public new AuthUserModel User { get; set; }

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepo userRepo)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.userRepo = userRepo;
            this.User = new();
        }

        public void OnGet(string redirectURL = "/")
        {
            this.redirectURL = redirectURL;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if(!ModelState.IsValid)
            {
                return Page();
            }

            if(User != null)
            {
                ApplicationUser? authUser = await userManager.FindByEmailAsync(User.Email);
                if ( authUser != null )
                {
                    ModelState.AddModelError("User.Email", "User with this email already exists");
                    return Page();
                }

                HVZ.Models.User dbUser = await userRepo.CreateUser(
                    $"{User.FirstName} {User.LastName}",
                    User.Email
                );

                authUser = new ApplicationUser
                {
                    FirstName = User.FirstName,
                    LastName = User.LastName,
                    Email = User.Email,
                    DatabaseId = dbUser.Id,
                    UserName = User.Email
                };

                IdentityResult result = await userManager.CreateAsync(
                    authUser,
                    User.Password
                );
                if(result.Succeeded)
                {
                    // Log the user in
                    Microsoft.AspNetCore.Identity.SignInResult signInResult = await signInManager.PasswordSignInAsync(
                        authUser, User.Password, false, false
                    );
                    if(signInResult.Succeeded)
                    {
                        return Redirect(this.redirectURL);
                    }
                }
                else
                {
                    // TODO: Delete dbUser from the Users database
                    foreach(IdentityError error in result.Errors)
                    {
                        System.Console.WriteLine($"{error.Code}:{error.Description}");
                    }
                }
            }

            return Page();
        }
    }
}