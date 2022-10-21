namespace HVZ.Datastore;
using HVZ.Models;
public interface IGameRepo
{
    /// <summary>
    /// Add a new game to the repo
    /// </summary>
    /// <param name="userid">The ID of the user who is creating this game</param>
    /// <returns>The newly created game</returns>
    public Task<Game> CreateGame(string userid);
    /// <summary>
    /// Find a game by its Id
    /// </summary>
    /// <returns>The game with the given ID, or Null if no game found</returns>
    public Task<Game?> FindById(string id);
}