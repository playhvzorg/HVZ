namespace HVZ.Persistence;
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
    public Task<Game?> FindGameById(string id);

    /// <summary>
    /// Find a game from its name
    /// </summary>
    /// <returns>The game with the given name, or Null if no game is found</returns>
    public Task<Game?> FindGameByName(string name);

    /// <summary>
    /// Find a player belonging to a specific game by their ID
    /// </summary>
    /// <param name="gameName"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<Player?> FindPlayerByUserId(string gameName, string userId);

    public event EventHandler<GameUpdatedEventArgs> GameCreated;
    public event EventHandler<GameUpdatedEventArgs> PlayerJoinedGame;
    public event EventHandler<GameUpdatedEventArgs> PlayerRoleChanged;
    public event EventHandler<GameUpdatedEventArgs> TagLogged;
    public event EventHandler<GameUpdatedEventArgs> GameUpdated;
    
}

public class GameUpdatedEventArgs : EventArgs
{
    public Game game { get; init; }
    public GameUpdatedEventArgs(Game g)
    {
        game = g;
    }
}