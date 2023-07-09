using HVZ.Web.Server.Identity;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace HVZ.Web.Server.Pages.Account
{
    public class ResetModel : PageModel
    {
        [FromQuery(Name = "requestId")]
        public string RequestId { get; set; }

        [FromQuery(Name = "userId")]
        public string UserId { get; set; }

        [BindProperty]
        public UpdatePasswordRequest RequestModel { get; set; }

        private readonly UserManager<ApplicationUser> _userManager;

        public ResetModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            RequestModel = new();
        }

        public IActionResult OnGet()
        {
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
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(appUser, RequestId, RequestModel.Password);
            if (result.Succeeded)
            {
                // TODO: Log
                return Redirect("/");
            }

            return Page();
        }

    }
}