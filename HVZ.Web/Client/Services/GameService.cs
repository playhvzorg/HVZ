using FluentResults;
using HVZ.Persistence.Models;
using HVZ.Web.Client.Interfaces;
using HVZ.Web.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace HVZ.Web.Client.Services
{
    public class GameService : IGameService
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IAuthService _auth;

        public GameService(HttpClient httpClient, JsonSerializerOptions options, IAuthService auth)
        {
            _http = httpClient;
            _jsonOptions = options;
            _auth = auth;
        }

        public async Task<Result<GameConfig>> GetGameConfig(string gameId)
        {
            var configResult = await _http.GetAsync($"/api/Game/{gameId}/config");

            if (!configResult.IsSuccessStatusCode)
            {
                if (configResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return await Logout();

                return Result.Fail(await configResult.Content.ReadAsStringAsync());
            }

            var gameConfig = await configResult.Content.ReadFromJsonAsync<GameConfig>(options: _jsonOptions);

            if (gameConfig is null)
                return Result.Fail("Could not deserialize result");

            return Result.Ok(gameConfig);
        }

        public async Task<Result<GameInfo>> GetGameInfo(string gameId)
        {
            var infoResult = await _http.GetAsync($"/api/Game/{gameId}/info");

            if (!infoResult.IsSuccessStatusCode)
            {
                if (infoResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return await Logout();

                return Result.Fail(await infoResult.Content.ReadAsStringAsync());
            }

            var gameInfo = await infoResult.Content.ReadFromJsonAsync<GameInfo>(options: _jsonOptions);

            if (gameInfo is null)
                return Result.Fail("Could not deserialize result");

            return Result.Ok(gameInfo);
        }

        public async Task<Result<IEnumerable<GameLogData>>> GetGameLog(string gameId)
        {
            var logResult = await _http.GetAsync($"/api/Game/{gameId}/events");

            if (!logResult.IsSuccessStatusCode)
            {
                if (logResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return await Logout();

                return Result.Fail(await logResult.Content.ReadAsStringAsync());
            }

            var eventLog = await logResult.Content.ReadFromJsonAsync<IEnumerable<GameLogData>>(options: _jsonOptions);

            if (eventLog is null)
                return Result.Fail("Could not deserialize result");

            return Result.Ok(eventLog);
        }

        public async Task<Result<IEnumerable<PlayerData>>> GetPlayers(string gameId)
        {
            var playersResult = await _http.GetAsync($"/api/Game/{gameId}/players");

            if (!playersResult.IsSuccessStatusCode)
            {
                if (playersResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return await Logout();

                return Result.Fail(await playersResult.Content.ReadAsStringAsync());
            }

            var playersList = await playersResult.Content.ReadFromJsonAsync<IEnumerable<PlayerData>>(options: _jsonOptions);

            if (playersList is null)
                return Result.Fail("Could not deserialize result");

            return Result.Ok(playersList);
        }

        public async Task<Result<TagResult>> LogTag(string gameId, string receiverId)
        {
            var tagResult = await _http.PostAsJsonAsync($"/api/Game/{gameId}/tag", new TagModel { ReceiverGameId = receiverId });
            if (tagResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return await Logout();

            var result = await tagResult.Content.ReadFromJsonAsync<TagResult>(_jsonOptions);
            if (result is not null)
                return Result.Ok(result);

            return Result.Fail("Could not deserialize response");
        }

        public async Task<Result<PlayerData?>> Me(string gameId)
        {
            var meResult = await _http.GetAsync($"/api/Game/{gameId}/myinfo");

            if (!meResult.IsSuccessStatusCode)
            {
                if (meResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return await Logout();

                return Result.Fail("Could not find player or game");
            }

            if (meResult.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return Result.Ok((PlayerData?)null);
            }

            var player = await meResult.Content.ReadFromJsonAsync<PlayerData?>(_jsonOptions);
            return Result.Ok(player);
        }

        public async Task<Result<PlayerData>> JoinGame(string gameId)
        {
            var joinResult = await _http.PostAsync($"/api/Game/{gameId}/join", null);

            if (!joinResult.IsSuccessStatusCode)
            {
                if (joinResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return await Logout();
            }

            var joinGame = await joinResult.Content.ReadFromJsonAsync<JoinGameResult>(_jsonOptions);

            if (joinGame is null)
                return Result.Fail("Could not deserialize response");

            if (joinGame.Succeeded)
                // CreatedPlayer will not be null if the operation succeeded, safe to ignore null
                return Result.Ok(joinGame.CreatedPlayer!);

            return Result.Fail(joinGame.Errors);
        }

        public async Task<Result<IEnumerable<UserData>>> GetOzPool(string gameId)
        {
            var ozPoolResult = await _http.GetAsync($"/api/Game/{gameId}/ozs/list");

            if (!ozPoolResult.IsSuccessStatusCode)
            {
                if (ozPoolResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return await Logout();

                return Result.Fail(await ozPoolResult.Content.ReadAsStringAsync());
            }

            var ozPool = await ozPoolResult.Content.ReadFromJsonAsync<IEnumerable<UserData>>(_jsonOptions);

            if (ozPool is null)
                return Result.Fail("Could not deserialize response");

            return Result.Ok(ozPool);
        }

        public async Task<Result<IEnumerable<UserData>>> SetRandomOzs(string gameId, int numRandomOzs)
        {
            var randomOzResult = await _http.PostAsJsonAsync($"/api/Game/{gameId}/randomoz", new RandomOzModel { NumRandomOzs = numRandomOzs });

            if (randomOzResult.IsSuccessStatusCode)
            {
                var result = await randomOzResult.Content.ReadFromJsonAsync<RandomOzResult>();
                if (result is null)
                    return Result.Fail("Could not deserialize result");

                if (result.Succeeded)
                    return Result.Ok(result.RandomOzs!); // Will not be null if result succeeded

                return Result.Fail(result.Error);
            }

            if (randomOzResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return await Logout();

            return Result.Fail(await randomOzResult.Content.ReadAsStringAsync());
        }

        public async Task<Result> SetPlayerToRole(string gameId, string userId, Player.gameRole role)
        {
            var setRoleResult = await _http.PostAsJsonAsync($"/api/Game/{gameId}/setrole",
                new SetGameRoleRequest
                {
                    UserId = userId,
                    Role = role
                },
                options: _jsonOptions);

            if (setRoleResult.IsSuccessStatusCode)
            {
                return Result.Ok();
            }

            if (setRoleResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return await Logout();

            return Result.Fail(await setRoleResult.Content.ReadAsStringAsync());
        }

        public async Task<Result<string>> RemovePlayerFromOzPool(string gameId, string userId)
        {
            return Result.Fail("Not implemented");
        }

        public async Task<Result<bool>> IsInOzPool(string gameId)
        {
            var inOzPoolResult = await _http.GetAsync($"/api/Game/{gameId}/ozs/inpool");

            string val = await inOzPoolResult.Content.ReadAsStringAsync();

            if (Boolean.TryParse(val, out bool result))
            {
                return Result.Ok(result);
            }

            return Result.Fail(await inOzPoolResult.Content.ReadAsStringAsync());
        }

        public async Task<Result> JoinOzPool(string gameId)
        {
            var joinResult = await _http.PostAsync($"/api/Game/{gameId}/ozs/join", null);

            if (joinResult.IsSuccessStatusCode)
                return Result.Ok();

            if (joinResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return await Logout();

            return Result.Fail(await joinResult.Content.ReadAsStringAsync());
        }

        public async Task<Result> LeaveOzPool(string gameId)
        {
            var leaveResult = await _http.PostAsync($"/api/Game/{gameId}/ozs/leave", null);

            if (leaveResult.IsSuccessStatusCode)
                return Result.Ok();

            if (leaveResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return await Logout();

            return Result.Fail(await leaveResult.Content.ReadAsStringAsync());

        }

        private async Task<Result> Logout()
        {
            await _auth.Logout();
            return Result.Fail("Unauthorized");
        }
    }
}
