using FluentResults;
using HVZ.Persistence.Models;
using HVZ.Web.Shared.Models;

namespace HVZ.Web.Client.Interfaces
{
    public interface IGameService
    {
        public Task<Result<IEnumerable<PlayerData>>> GetPlayers(string gameId);
        public Task<Result<IEnumerable<GameLogData>>> GetGameLog(string gameId);
        public Task<Result<GameInfo>> GetGameInfo(string gameId);
        public Task<Result<GameConfig>> GetGameConfig(string gameId);
        public Task<Result<TagResult>> LogTag(string gameId, string receiverId);
        public Task<Result<PlayerData?>> Me(string gameId);
        public Task<Result<PlayerData>> JoinGame(string gameId);
        public Task<Result<IEnumerable<UserData>>> GetOzPool(string gameId);
        public Task<Result<IEnumerable<UserData>>> SetRandomOzs(string gameId, int numRandomOzs);
        public Task<Result> SetPlayerToRole(string gameId, string userId, Player.gameRole role);
        public Task<Result<string>> RemovePlayerFromOzPool(string gameId, string userId);
        public Task<Result<bool>> IsInOzPool(string gameId);
        public Task<Result> JoinOzPool(string gameId);
        public Task<Result> LeaveOzPool(string gameId);
    }
}
