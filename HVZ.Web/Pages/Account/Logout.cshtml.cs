namespace HVZ.Web.Pages;

using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize]
public class LogoutModel : PageModel {
    private ILogger<LogoutModel> logger;
    private SignInManager<ApplicationUser> signInManager;

    public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
    {
        this.signInManager = signInManager;
        this.logger = logger;
    }


    public async Task<IActionResult> OnGet()
    {
        await signInManager.SignOutAsync();
        logger.LogDebug("Successful Logout");
        return Redirect("/");
    }
}