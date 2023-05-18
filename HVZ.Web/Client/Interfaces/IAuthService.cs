using HVZ.Web.Shared.Models;

namespace HVZ.Web.Client.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="registerModel">Form data</param>
        /// <returns><see cref="RegisterResult"/> with either success or errors</returns>
        public Task<RegisterResult> Register(RegisterModel registerModel);

        /// <summary>
        /// Login as an existing user
        /// </summary>
        /// <param name="loginModel">Form data</param>
        /// <returns><see cref="LoginResult"/> with either success or error</returns>
        public Task<LoginResult> Login(LoginModel loginModel);

        public Task Logout();
    }
}
