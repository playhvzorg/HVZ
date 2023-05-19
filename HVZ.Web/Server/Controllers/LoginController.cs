using HVZ.Web.Server.Identity;
using HVZ.Web.Server.Services.Settings;
using HVZ.Web.Shared.Models;
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
    public class LoginController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signinManager;
        private readonly JwtConfig _jwtConfig;

        public LoginController(SignInManager<ApplicationUser> signinManager, JwtConfig jwtConfig)
        {
            _signinManager = signinManager;
            _jwtConfig = jwtConfig;
        }

        /// <summary>
        /// Login an existing user
        /// </summary>
        /// <param name="login"></param>
        /// <returns><see cref="LoginResult"/></returns>
        /// <response code="200">Returns a LoginResult object</response>
        /// <response code="400">Missing parameters or username/password is invalid</response>
        [HttpPost]
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

            var result = await _signinManager.PasswordSignInAsync(login.Email, login.Password, false, false);

            if (!result.Succeeded)
            {
                return BadRequest(new LoginResult { Succeeded = false, Error = "Username and password are invalid." });
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, login.Email)
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
    }
}
