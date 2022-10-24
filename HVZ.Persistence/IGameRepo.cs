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
    /// Find a player in a game by their global UserId
    /// </summary>
    /// <returns>The player with the given userId, or Null if no player is found</returns>
    public Task<Player?> FindPlayerByUserId(string gameName, string userId);

    /// <summary>
    /// Find a player in a game by their game-specific ID
    /// </summary>
    /// <returns>The player with the given userId, or Null if no player is found</returns>
    public Task<Player?> FindPlayerByGameId(string gameName, string gameId);

    /// <summary>
    /// Add a new player to an existing game
    /// </summary>
    public Task<Game> AddPlayer(string gameName, string userId);

    /// <summary>
    /// Sets isActive for a game
    /// </summary>
    public Task<Game> SetActive(string gameName, bool active);

    /// <summary>
    /// Set the <see cref="HVZ.Models.Player.gameRole"/> of a player
    /// </summary>
    public Task<Game> SetPlayerToRole(string gameName, string userId, Player.gameRole role);

    /// <summary>
    /// Log a tag in the specified game
    /// </summary>
    public Task<Game> LogTag(string gameName, string taggerUserId, string taggeeGameId);

    /// <summary>
    /// Event that fires when a new game is created
    /// </summary>
    public event EventHandler<GameUpdatedEventArgs> GameCreated;
    /// <summary>
    /// Event that fires when a player joins a game
    /// </summary>
    public event EventHandler<GameUpdatedEventArgs> PlayerJoinedGame;
    /// <summary>
    /// Event that fires when a player's role is changed for a game
    /// </summary>
    public event EventHandler<GameUpdatedEventArgs> PlayerRoleChanged;
    /// <summary>
    /// Event that first when a tag is logged in a game
    /// </summary>
    public event EventHandler<GameUpdatedEventArgs> TagLogged;
    /// <summary>
    /// Event that fires when a game is updated, such as changing isActive status
    /// </summary>
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