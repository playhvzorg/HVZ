namespace HVZ.Web.Pages;

using System.Web;
using Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using Persistence;
using Services;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

public class RegisterModel : PageModel {
    private EmailService emailService;
    private ILogger<RegisterModel> logger;
    private string redirectURL = "/";
    private SignInManager<ApplicationUser> signInManager;

    private UserManager<ApplicationUser> userManager;
    private IUserRepo userRepo;

    public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepo userRepo, ILogger<RegisterModel> logger, EmailService email)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.userRepo = userRepo;
        emailService = email;
        UserModel = new AuthUserModel();
        this.logger = logger;
    }

    [BindProperty]
    public AuthUserModel UserModel { get; set; }

    public void OnGet(string redirectURL = "/")
    {
        this.redirectURL = redirectURL;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (UserModel != null)
        {
            ApplicationUser? authUser = await userManager.FindByEmailAsync(UserModel.Email);
            if (authUser != null)
            {
                ModelState.AddModelError("UserModel.Email", "This email is already in use.");
                return Page();
            }

            User dbUser;

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
                SignInResult signInResult = await signInManager.PasswordSignInAsync(
                    authUser, UserModel.Password, false, false
                );
                logger.LogInformation($"New user created\nName:\t{dbUser.FullName}\nId:\t{dbUser.Id}");
                if (signInResult.Succeeded)
                    return Redirect(redirectURL);
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