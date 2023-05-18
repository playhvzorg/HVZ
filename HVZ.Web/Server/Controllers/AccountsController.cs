using HVZ.Persistence;
using HVZ.Web.Server.Identity;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HVZ.Web.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRepo _userRepo;

        public AccountsController(UserManager<ApplicationUser> userManager, IUserRepo userRepo)
        {
            _userManager = userManager;
            _userRepo = userRepo;
        }

        // Stuff
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] RegisterModel model)
        {
            // Error if any of the fields are null
            if (model.FullName is null ||
                model.Email is null ||
                model.Password is null ||
                model.ConfirmPassword is null)
            {
                return BadRequest(new RegisterResult
                {
                    Succeeded = false,
                    Errors = new List<string>
                    {
                        "Request Error: Requset cannot contain null values"
                    }
                });
            }

            var hvzUser = await _userRepo.CreateUser(model.FullName, model.Email);
            var result = await _userManager.CreateAsync(new ApplicationUser
            {
                FullName = hvzUser.FullName,
                Email = hvzUser.Email,
                DatabaseId = hvzUser.Id
            });

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(x => x.Description);
                await _userRepo.DeleteUser(hvzUser.Id);
                return Ok(new RegisterResult { Succeeded = false, Errors = errors });
            }

            return Ok(new RegisterResult { Succeeded = true });
        }
    }
}
