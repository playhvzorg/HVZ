using FluentResults;
using HVZ.Web.Client.Interfaces;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using System.Text.Json;

namespace HVZ.Web.Client.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        private UserData? _currentUser;

        public UserService(HttpClient http, JsonSerializerOptions options)
        {
            _http = http;
            _jsonOptions = options;
        }

        public async Task<Result<bool>> EmailIsConfirmed()
        {
            await Task.Delay(1200);

            return true;
        }

        public async Task<Result<UserData>> GetCurrentUser()
        {
            if (_currentUser is not null)
                return Result.Ok(_currentUser);

            var userResult = await _http.GetFromJsonAsync<UserData>("/api/accounts/me");
            if (userResult is not null)
            {
                _currentUser = userResult;
                return Result.Ok(userResult);
            }

            return Result.Fail("Could not find user result");
        }

        public Task<Result<UserData>> GetUserById(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<bool>> SetImage(IBrowserFile image)
        {
            // RequestImageFileAsync preserves aspect ratio. Max size can be lowered to increase upload speeds but risks losing image quality
            IBrowserFile? imageFile = await image.RequestImageFileAsync(image.ContentType, 512, 512);
            if (imageFile is null) return Result.Fail("Not a valid image file");

            Console.WriteLine(imageFile.ContentType);

            StreamContent fileContent = new StreamContent(imageFile.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);

            MultipartFormDataContent content = new MultipartFormDataContent();
            content.Add(
                content: fileContent,
                name: "\"file\"",
                fileName: image.Name);

            var response = await _http.PostAsync("image/user/upload", content);

            if (response.IsSuccessStatusCode)
                return Result.Ok(true);

            return Result.Fail(await response.Content.ReadAsStringAsync());
        }
    }
}
