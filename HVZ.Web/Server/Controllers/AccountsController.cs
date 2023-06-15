using HVZ.Persistence;
using HVZ.Persistence.Models;
using HVZ.Web.Server.Identity;
using HVZ.Web.Server.Services.Settings;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HVZ.Web.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtConfig _jwtConfig;
        private readonly IUserRepo _userRepo;

        public AccountsController(UserManager<ApplicationUser> userManager, IUserRepo userRepo, SignInManager<ApplicationUser> signInManager, JwtConfig jwtConfig)
        {
            _userManager = userManager;
            _userRepo = userRepo;
            _signInManager = signInManager;
            _jwtConfig = jwtConfig;
        }

        /// <summary>
        /// Creates a new user account
        /// </summary>
        /// <param name="model"></param>
        /// <returns><see cref="RegisterResult"/> containing the result of the operation</returns>
        /// <response code="200">Returns the <see cref="RegisterResult"/> object</response>
        /// <response code="401">Missing input parameter or error creating new user</response>
        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
                return BadRequest(new RegisterResult { Succeeded = false, Errors = errors });
            }

            return Ok(new RegisterResult { Succeeded = true });
        }

        /// <summary>
        /// Login an existing user
        /// </summary>
        /// <param name="login"></param>
        /// <returns><see cref="LoginResult"/></returns>
        /// <response code="200">Returns a LoginResult object</response>
        /// <response code="400">Missing parameters or username/password is invalid</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginModel login)
        {
            if (login.Email is null || login.Password is null)
            {
                return BadRequest(new LoginResult
                {
                    Succeeded = false,
                    Error = "Request Error: Requset cannot contain null values"
                });
            }

            var result = await _signInManager.PasswordSignInAsync(login.Email, login.Password, false, false);

            if (!result.Succeeded)
            {
                return BadRequest(new LoginResult { Succeeded = false, Error = "Username and password are invalid." });
            }

            var user = await _userRepo.GetUserByEmail(login.Email);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, login.Email),
                new Claim("DatabaseId", user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.JwtSecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddDays(_jwtConfig.JwtExpiryInDays);

            var token = new JwtSecurityToken(
                _jwtConfig.JwtIssuer,
                _jwtConfig.JwtAudience,
                claims,
                expires: expiry,
                signingCredentials: creds
            );

            return Ok(new LoginResult
            {
                Succeeded = true,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }

        /// <summary>
        /// Logout currently signed in user
        /// </summary>
        /// <returns></returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok();
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserData>> Me()
        {
            string userId = this.User.Claims.FirstOrDefault(c => c.Type == "DatabaseId")?.Value ?? string.Empty;

            if (userId == string.Empty)
            {
                return BadRequest();
            }

            User user = await _userRepo.GetUserById(userId);

            return Ok(new UserData { Email = user.Email, FullName = user.FullName, UserId = user.Id });
        }
    }
}
