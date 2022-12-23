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
        private ILogger<RegisterModel> logger;
        private string redirectURL = "/";

        [BindProperty]
        public AuthUserModel UserModel { get; set; }

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepo userRepo, ILogger<RegisterModel> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.userRepo = userRepo;
            this.UserModel = new();
            this.logger = logger;
        }

        public void OnGet(string redirectURL = "/")
        {
            this.redirectURL = redirectURL;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (UserModel != null)
            {
                ApplicationUser? authUser = await userManager.FindByEmailAsync(UserModel.Email);
                if (authUser != null)
                {
                    ModelState.AddModelError("User.Email", "User with this email already exists");
                    return Page();
                }

                HVZ.Models.User dbUser = await userRepo.CreateUser(
                    UserModel.FullName,
                    UserModel.Email
                );

                authUser = new ApplicationUser
                {
                    FullName = UserModel.FullName,
                    Email = UserModel.Email,
                    DatabaseId = dbUser.Id,
                    UserName = UserModel.Email
                };

                IdentityResult result = await userManager.CreateAsync(
                    authUser,
                    UserModel.Password
                );
                if (result.Succeeded)
                {
                    // Log the user in
                    Microsoft.AspNetCore.Identity.SignInResult signInResult = await signInManager.PasswordSignInAsync(
                        authUser, UserModel.Password, false, false
                    );
                    logger.LogDebug($"New user created\nName:\t{dbUser.FullName}\nId:\t{dbUser.Id}");
                    if (signInResult.Succeeded)
                    {
                        return Redirect(this.redirectURL);
                    }
                }
                else
                {
                    await userRepo.DeleteUser(dbUser.Id);
                    string errors = "";
                    foreach (IdentityError error in result.Errors)
                    {
                        errors += $"\n{error.Code}:{error.Description}";
                    }
                    logger.LogError("Critical error creating user identity!{}", errors);
                }
            }

            return Page();
        }
    }
}