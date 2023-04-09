namespace HVZ.Persistence;
using HVZ.Persistence.Models;

public interface IGameRepo
{
    /// <summary>
    /// Add a new game to the repo. This is usually done from the OrgRepo, see <see cref="IOrgRepo.CreateGame"/>
    /// </summary>
    public Task<Game> CreateGame(string Name, string creatorUserId, string orgid, int ozMaxTags = 3);
    /// <summary>
    /// Find a game by its Id
    /// </summary>
    /// <returns>The game with the given ID, or Null if no game is found</returns>
    public Task<Game?> FindGameById(string id);

    /// <summary>
    /// Get a game by its Id
    /// </summary>
    /// <returns>The game with the given ID. Throws an exception when no game found</returns>
    public Task<Game> GetGameById(string id);

    /// <summary>
    /// Find a game from its name
    /// </summary>
    /// <returns>The game with the given name, or Null if no game is found</returns>
    public Task<Game?> FindGameByName(string name);

    /// <summary>
    /// Get a game by its name
    /// </summary>
    /// <returns>The game with the given name. Throws an exception when no game found</returns>
    public Task<Game> GetGameByName(string name);

    /// <summary>
    /// Find a player in a game by their global UserId
    /// </summary>
    /// <returns>The player with the given userId, or Null if no player is found</returns>
    public Task<Player?> FindPlayerByUserId(string gameId, string userId);

    /// <summary>
    /// Get a player by their global UserId
    /// </summary>
    ///<returns>The player with the given userId. Throws an exception when no player can be found</returns>
    public Task<Player> GetPlayerByUserId(string gameId, string userId);

    /// <summary>
    /// Find a player in a game by their game-specific ID
    /// </summary>
    /// <returns>The player with the given game-specific ID, or Null if no player is found</returns>
    public Task<Player?> FindPlayerByGameId(string gameId, string userGameId);

    /// <summary>
    /// Get a player in a game by their game-specific ID
    /// </summary>
    /// <returns>The player with the given game-specific ID. Throws an exception when no player can be found</returns>
    public Task<Player> GetPlayerByGameId(string gameId, string userGameId);

    /// <summary>
    /// Add a new player to an existing game
    /// </summary>
    public Task<Game> AddPlayer(string gameId, string userId);

    /// <summary>
    /// Set the <see cref="Game.GameStatus"/> of a game
    /// </summary>
    //public Task<Game> SetGameStatus(string gameId, Game.GameStatus status, string instigatorId);

    /// <summary>
    /// Set the <see cref="Game.GameStatus"/> of a New game to Active and set the startedAt field the current <see cref="NodaTime.Instant"/>
    /// </summary>
    /// <param name="gameId">The global ID for the game</param>
    /// <param name="instigatorId">The global Id for the user initiating the change</param>
    /// <returns>The updated game. Throws exception if the <see cref="Game.GameStatus"/> is not New</returns>
    /// <exception cref="ArgumentException"></exception>
    public Task<Game> StartGame(string gameId, string instigatorId);

    /// <summary>
    /// Set whether the <see cref="Game.GameStatus"/> is set to Paused (true) or Active (false)
    /// </summary>
    /// <param name="gameId">The global ID for the game</param>
    /// <param name="instigatorId">The glboal Id for the user initiating the change</param>
    /// <returns>The updated game. Throws exception if the <see cref="Game.GameStatus"/> is not Active</returns>
    /// <exception cref="ArgumentException"></exception>
    public Task<Game> PauseGame(string gameId, string instigatorId);

    /// <summary>
    /// Set whether the <see cref="Game.GameStatus"/> is set to Paused (true) or Active (false)
    /// </summary>
    /// <param name="gameId">The global ID for the game</param>
    /// <param name="instigatorId">The glboal Id for the user initiating the change</param>
    /// <returns>The updated game. Throws exception if the <see cref="Game.GameStatus"/> is not Paused</returns>
    /// <exception cref="ArgumentException"></exception>
    public Task<Game> ResumeGame(string gameId, string instigatorId);

    /// <summary>
    /// Set the <see cref="Game.GameStatus"/> to Ended and set the endedAt field to the current <see cref="NodaTime.Instant"/>
    /// </summary>
    /// <param name="gameId">The global ID for the game</param>
    /// <param name="instigatorId">The global Id for the user initiating the change</param>
    /// <returns>The updated game. Throws exception if the <see cref="Game.GameStatus"/> is New</returns>
    /// <exception cref="ArgumentException"></exception>
    public Task<Game> EndGame(string gameId, string instigatorId);

    /// <summary>
    /// Set the <see cref="HVZ.Persistence.Models.Player.gameRole"/> of a player
    /// </summary>
    /// <param name="instigatorId">User who is causing the player to change role</param>
    public Task<Game> SetPlayerToRole(string gameId, string userId, Player.gameRole role, string instigatorId);

    /// <summary>
    /// Log a tag in the specified game
    /// </summary>
    public Task<Game> LogTag(string gameId, string taggerUserId, string tagRecieverGameId);

    /// <summary>
    /// Get an IEnumerable of games which contain the given user.
    /// </summary>
    /// <param name="limit">Max amount of games to return. Unlimited if not provided</param>
    /// <returns>An IEnumerable of games. May be empty.</returns>
    public Task<List<Game>> GetGamesWithUser(string userId, int? limit = null);

    /// <summary>
    /// Get an IEnumerable of games which contain the given user and are active.
    /// </summary>
    /// <param name="limit">Max amount of games to return. Unlimited if not provided</param>
    /// <returns>An IEnumerable of games. May be empty.</returns>
    public Task<List<Game>> GetCurrentGamesWithUser(string userId, int? limit = null);

    /// <summary>
    /// Get the event log
    /// </summary>
    public Task<List<GameEventLog>> GetGameEventLog(string gameId);

    /// <summary>
    /// Add a player to the game's OZ pool
    /// </summary>
    /// <param name="gameId">ID for the game</param>
    /// <param name="userId">Player's UserId</param>
    public Task<Game> AddPlayerToOzPool(string gameId, string userId);

    /// <summary>
    /// Remove a player from the game's OZ pool
    /// </summary>
    /// <param name="gameId">ID for the game</param>
    /// <param name="userId">Player's UserId</param>
    public Task<Game> RemovePlayerFromOzPool(string gameId, string userId);

    /// <summary>
    /// Select the specified number of random players from the game's OZ pool, set them to OZ, and remove them from the OZ pool
    /// </summary>
    /// <param name="gameId">ID for the game</param>
    /// <param name="count">Number of random OZs</param>
    /// <param name="instigatorId">UserId for user responsible for this action</param>
    public Task<Game> AssignRandomOzs(string gameId, int count, string instigatorId);

    /// <summary>
    /// Set the maximum number of tags a player can get as an OZ
    /// </summary>
    /// <param name="gameId">ID for the game</param>
    /// <param name="count">Maximum number of OZ tags</param>
    /// <param name="instigatorId">UserId for the user responsible for this action</param>
    public Task<Game> SetOzTagCount(string gameId, int count, string instigatorId);

    /// <summary>
    /// Get the maximum number of tags a player can get as an OZ
    /// </summary>
    public Task<int> GetOzTagCount(string gameId);

    /// <summary>
    /// Event that fires when a new game is created
    /// </summary>
    public event EventHandler<GameUpdatedEventArgs> GameCreated;

    /// <summary>
    /// Event that fires when a player joins a game
    /// </summary>
    public event EventHandler<PlayerUpdatedEventArgs> PlayerJoinedGame;

    /// <summary>
    /// Event that fires when a player's role is changed for a game
    /// </summary>
    public event EventHandler<PlayerRoleChangedEventArgs> PlayerRoleChanged;

    /// <summary>
    /// Event that fires when a tag is logged in a game
    /// </summary>
    public event EventHandler<TagEventArgs> TagLogged;

    /// <summary>
    /// Event that fires when a game's isActive status is changed
    /// </summary>
    public event EventHandler<GameStatusChangedEvent> GameActiveStatusChanged;
    /// <summary>
    /// Event that fires when a game's settings are changed
    /// </summary>
    public event EventHandler<GameUpdatedEventArgs> GameSettingsChanged;
    /// <summary>
    /// Event that fires when a player joins the OZ pool
    /// </summary>
    public event EventHandler<OzPoolUpdatedEventArgs> PlayerJoinedOzPool;
    /// <summary>
    /// Event that fires when a player leaves the OZ pool
    /// </summary>
    public event EventHandler<OzPoolUpdatedEventArgs> PlayerLeftOzPool;
    /// <summary>
    /// Event that fires when random OZs are set
    /// </summary>
    public event EventHandler<RandomOzEventArgs> RandomOzsSet;
}

public class GameUpdatedEventArgs : EventArgs
{
    public Game game { get; init; }
    public string updatorId { get; init; }

    public GameUpdatedEventArgs(Game g, string id)
    {
        game = g;
        updatorId = id;
    }
}

public class PlayerUpdatedEventArgs : EventArgs
{
    public Game game { get; init; }
    public Player player { get; init; }

    public PlayerUpdatedEventArgs(Game g, Player p)
    {
        game = g;
        player = p;
    }
}

public class PlayerRoleChangedEventArgs : EventArgs
{
    public Game game { get; init; }
    public Player player { get; init; }
    public string instigatorId { get; init; }
    public Player.gameRole Role { get; init; }

    public PlayerRoleChangedEventArgs(Game g, Player p, string instigatorid, Player.gameRole role)
    {
        game = g;
        player = p;
        instigatorId = instigatorid;
        Role = role;
    }
}

public class TagEventArgs : EventArgs
{
    public Game game { get; init; }
    public Player Tagger { get; init; }
    public Player TagReciever { get; init; }

    public TagEventArgs(Game g, Player tagger, Player tagreciever)
    {
        game = g;
        Tagger = tagger;
        TagReciever = tagreciever;
    }
}

public class GameStatusChangedEvent : EventArgs
{
    public Game game { get; init; }
    public string updatorId { get; init; }
    public Game.GameStatus Status { get; init; }

    public GameStatusChangedEvent(Game g, string id, Game.GameStatus status)
    {
        game = g;
        updatorId = id;
        Status = status;
    }
}

public class OzPoolUpdatedEventArgs : EventArgs
{
    public Game game { get; init; }
    public string playerId { get; init; }
    public OzPoolUpdatedEventArgs(Game game, string playerId)
    {
        this.game = game;
        this.playerId = playerId;
    }
}

public class RandomOzEventArgs : EventArgs
{
    public Game game { get; init; }
    public string[] randomOzIds { get; init; }
    public string instigatorId { get; init; }
    public RandomOzEventArgs(Game game, string[] randomOzIds, string instigatorId)
    {
        this.game = game;
        this.randomOzIds = randomOzIds;
        this.instigatorId = instigatorId;
    }
}