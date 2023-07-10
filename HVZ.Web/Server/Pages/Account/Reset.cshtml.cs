using HVZ.Web.Server.Identity;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HVZ.Web.Server.Pages.Account
{
    public class ResetModel : PageModel
    {
        [FromQuery(Name = "requestId")]
        public string RequestId { get; set; } = default!;

        [FromQuery(Name = "userId")]
        public string UserId { get; set; } = default!;

        [BindProperty]
        public UpdatePasswordRequest RequestModel { get; set; }

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ResetModel> _logger;

        public ResetModel(UserManager<ApplicationUser> userManager, ILogger<ResetModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
            RequestModel = new();
        }

        public IActionResult OnGet()
        {
            // Web developer moment
            ModelState.Clear();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var appUser = await _userManager.FindByIdAsync(UserId);
            if (appUser is null)
            {
                _logger.LogError("Could not find user with ID: {userId}", UserId);
                return NotFound();
            }

            // Error if the UserName is the user's full name (This can happen, don't ask me why...)
            appUser.UserName = appUser.Email;

            var result = await _userManager.ResetPasswordAsync(appUser, RequestId, RequestModel.Password!);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {email} has changed their password", appUser.Email);
                return Redirect("/");
            }
            else
            {
                foreach(var error in result.Errors)
                {
                    _logger.LogError("Error resetting password for {email}\n\t{errorCode}: {errorDescription}", appUser.Email, error.Code, error.Description);
                }
            }

            return Page();
        }

    }
}