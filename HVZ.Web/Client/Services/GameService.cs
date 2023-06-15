using FluentResults;
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

        public GameService(HttpClient httpClient, JsonSerializerOptions options)
        {
            _http = httpClient;
            _jsonOptions = options;
        }

        public async Task<Result<GameConfig>> GetGameConfig(string gameId)
        {
            var configResult = await _http.GetAsync($"/api/Game/{gameId}/config");

            if (!configResult.IsSuccessStatusCode)
            {
                if (configResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return Result.Fail("Unauthenticated");
                }
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
                {
                    return Result.Fail("Unauthenticated");
                }
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
                {
                    return Result.Fail("Unauthenticated");
                }
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
                {
                    return Result.Fail("Unauthenticated");
                }
                return Result.Fail(await playersResult.Content.ReadAsStringAsync());
            }

            var playersList = await playersResult.Content.ReadFromJsonAsync<IEnumerable<PlayerData>>(options: _jsonOptions);

            if (playersList is null)
                return Result.Fail("Could not deserialize result");

            return Result.Ok(playersList);
        }

        public Task<Result<string>> LogTag(string gameId, string receiverId)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<PlayerData?>> Me(string gameId)
        {
            var meResult = await _http.GetAsync($"/api/Game/{gameId}/myinfo");

            if (!meResult.IsSuccessStatusCode)
            {
                if (meResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return Result.Fail("Unauthenticated");
                }
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
                {
                    return Result.Fail("Unauthenticated");
                }
            }

            var joinGame = await joinResult.Content.ReadFromJsonAsync<JoinGameResult>(_jsonOptions);

            if (joinGame is null)
                return Result.Fail("Could not deserialize response");

            if (joinGame.Succeeded)
                // CreatedPlayer will not be null if the operation succeeded, safe to ignore null
                return Result.Ok(joinGame.CreatedPlayer!);

            return Result.Fail(joinGame.Errors);
        }
    }
}
