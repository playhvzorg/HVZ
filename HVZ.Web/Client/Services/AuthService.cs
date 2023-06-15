using Blazored.LocalStorage;
using HVZ.Web.Client.Interfaces;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HVZ.Web.Client.Services
{
    public class AuthService : IAuthService
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

        public async Task<RegisterResult> Register(RegisterModel registerModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/accounts/create", registerModel);
            var result = await response.Content.ReadFromJsonAsync<RegisterResult>();
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result), "Could not deserialize response");
            }

            return result;
        }

        public async Task<LoginResult> Login(LoginModel loginModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/accounts/login", loginModel);
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

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            ((ApiAuthenticationStateProvider)_authStateProvider).MarkUserAsLoggedOut();
            await _httpClient.PostAsync("/accounts/logout", null);
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
