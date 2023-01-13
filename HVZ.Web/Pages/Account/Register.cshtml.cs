using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HVZ.Web.Identity.Models;
using HVZ.Web.Services;
using HVZ.Persistence;

namespace HVZ.Web.Pages
{
    public class RegisterModel : PageModel
    {

        private UserManager<ApplicationUser> userManager;
        private SignInManager<ApplicationUser> signInManager;
        private IUserRepo userRepo;
        private ILogger<RegisterModel> logger;
        private EmailService emailService;
        private string redirectURL = "/";

        [BindProperty]
        public AuthUserModel UserModel { get; set; }

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepo userRepo, ILogger<RegisterModel> logger, EmailService email)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.userRepo = userRepo;
            this.emailService = email;
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
                    ModelState.AddModelError("UserModel.Email", "This email is already in use.");
                    return Page();
                }

                HVZ.Models.User dbUser;

                try
                {
                    dbUser = await userRepo.CreateUser(
                        UserModel.FullName,
                        UserModel.Email
                    );
                }
                catch (ArgumentException e)
                {
                    ModelState.AddModelError("UserModel.Email", e.Message);
                    return Page();
                }

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
                    // Send email confirmation
                    string token = await userManager.GenerateEmailConfirmationTokenAsync(authUser);
                    await emailService.SendVerificationEmailAsync(authUser.Email, authUser.FullName, HttpUtility.UrlEncode(token));

                    // Log the user in
                    Microsoft.AspNetCore.Identity.SignInResult signInResult = await signInManager.PasswordSignInAsync(
                        authUser, UserModel.Password, false, false
                    );
                    logger.LogInformation($"New user created\nName:\t{dbUser.FullName}\nId:\t{dbUser.Id}");
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