using HVZ.Web.Server.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HVZ.Web.Server.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        [FromQuery(Name = "requestId")]
        public string? RequestId { get; set; }

        [FromQuery(Name = "userId")]
        public string? UserId { get; set; }

        public bool Loading { get; set; } = true;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ActionResult> OnGetAsync()
        {
            // Validate 
            if (RequestId is null)
                return Page();

            if (UserId is null)
                return Page();

            var user = await _userManager.FindByIdAsync(UserId);
            if (user is null)
                return NotFound();
            
            var result = await _userManager.ConfirmEmailAsync(user, RequestId);

            Loading = false;

            if (result.Succeeded)
                return Page();
            
            foreach(var error in result.Errors)
            {
                _logger.LogError("Error confirming email for {email}\n\t{errorCode}: {errorDescription}", user.Email, error.Code, error.Description);
            }
            return Page();
        }

    }
}