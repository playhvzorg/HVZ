using Blazored.LocalStorage;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HVZ.Web.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
            _localStorage = localStorage;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="registerModel">Form data</param>
        /// <returns><see cref="RegisterResult"/> with either success or errors</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<RegisterResult> Register(RegisterModel registerModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/accounts", registerModel);
            var result = await response.Content.ReadFromJsonAsync<RegisterResult>();
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result), "Could not deserialize response");
            }

            return result;
        }

        /// <summary>
        /// Login as an existing user
        /// </summary>
        /// <param name="loginModel">Form data</param>
        /// <returns><see cref="LoginResult"/> with either success or error</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<LoginResult> Login(LoginModel loginModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/login", loginModel);
            var result = await response.Content.ReadFromJsonAsync<LoginResult>();
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result), "Could not deserialize response");
            }

            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

            await _localStorage.SetItemAsync("authToken", result.Token);
            ((ApiAuthenticationStateProvider)_authStateProvider).MarkUserAsAuthenticated(loginModel.Email!);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.Token);

            return result;
        }

        /// <summary>
        /// Logout the current loggedin user
        /// </summary>
        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            ((ApiAuthenticationStateProvider)_authStateProvider).MarkUserAsLoggedOut();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
