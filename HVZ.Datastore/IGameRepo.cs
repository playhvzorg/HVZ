namespace HVZ.Datastore;
using HVZ.Models;
public interface IGameRepo
{
    /// <summary>
    /// Add a new game to the repo
    /// </summary>
    /// <param name="userid">The ID of the user who is creating this game</param>
    /// <returns>The newly created game</returns>
    public Task<Game> CreateGame(string Name, string userid);
    /// <summary>
    /// Find a game by its Id
    /// </summary>
    /// <returns>The game with the given ID, or Null if no game is found</returns>
    public Task<Game?> FindById(string id);

    /// <summary>
    /// Find a game from its name
    /// </summary>
    /// <returns>The game with the given name, or Null if no game is found</returns>
    public Task<Game?> FindByName(string name);
}