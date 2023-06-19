using FluentResults;
using HVZ.Web.Client.Interfaces;
using HVZ.Web.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace HVZ.Web.Client.Services
{
    public class OrgService : IOrgService
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public OrgService(HttpClient http, JsonSerializerOptions jsonOption)
        {
            _http = http;
            _jsonOptions = jsonOption;
        }

        public async Task<Result<string>> CreateGame(string orgId, CreateGameRequest request)
        {
            var result = await _http.PostAsJsonAsync($"/api/Org/{orgId}/game/create", request);

            try
            {
                var resultObject = await result.Content.ReadFromJsonAsync<CreateGameResult>();

                if (resultObject is null)
                    return Result.Fail("Could not deserialize response");

                if (resultObject.Succeeded)
                    return Result.Ok(resultObject.GameId!);

                return Result.Fail(resultObject.Error!);
            }
            catch
            {
                return Result.Fail("Internal server error occurred");
            }

        }

        public async Task<Result<OrgAuthorization>> GetAuthorization(string orgId)
        {
            var authorizationResult = await _http.GetAsync($"/api/Org/{orgId}/authorization");
            if (authorizationResult.IsSuccessStatusCode)
            {
                var authorization = await authorizationResult.Content.ReadFromJsonAsync<OrgAuthorization>(options: _jsonOptions);
                if (authorization is not null)
                    return Result.Ok(authorization);
                return Result.Fail("Could not deserialize response");
            }

            return Result.Fail(await authorizationResult.Content.ReadAsStringAsync());
        }

        public async Task<Result<string>> GetOrgIdFromUrl(string url)
        {
            var result = await _http.GetAsync($"/api/Org/{url}/id");

            if (result.IsSuccessStatusCode)
            {
                return Result.Ok(await result.Content.ReadAsStringAsync());
            }

            return Result.Fail(await result.Content.ReadAsStringAsync());
        }

        public async Task<Result<OrgInfo>> GetOrgInfo(string orgId)
        {
            var result = await _http.GetAsync($"/api/Org/{orgId}/info");

            if (result.IsSuccessStatusCode)
            {
                var resultObject = await result.Content.ReadFromJsonAsync<OrgInfo>(_jsonOptions);
                if (resultObject is not null)
                {
                    return resultObject;
                }
                return Result.Fail("Could not deserialize response");
            }

            return Result.Fail(await result.Content.ReadAsStringAsync());
        }

        public Task<Result<bool>> IsAdmin(string? userId)
        {
            throw new NotImplementedException();
        }

        public Task<Result<bool>> IsMod(string? userId)
        {
            throw new NotImplementedException();
        }
    }
}
