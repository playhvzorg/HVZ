using FluentResults;
using HVZ.Web.Shared.Models;

namespace HVZ.Web.Client.Interfaces
{
    public interface IGameService
    {
        public Task<Result<IEnumerable<PlayerData>>> GetPlayers(string gameId);
        public Task<Result<IEnumerable<GameLogData>>> GetGameLog(string gameId);
        public Task<Result<GameInfo>> GetGameInfo(string gameId);
        public Task<Result<GameConfig>> GetGameConfig(string gameId);
        public Task<Result<string>> LogTag(string gameId, string receiverId);
        public Task<Result<PlayerData?>> Me(string gameId);
        public Task<Result<PlayerData>> JoinGame(string gameId);

    }
}
