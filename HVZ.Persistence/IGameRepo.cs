namespace HVZ.Persistence;

using Models;

public interface IGameRepo {
    /// <summary>
    ///     Add a new game to the repo. This is usually done from the OrgRepo, see <see cref="IOrgRepo.CreateGame" />
    /// </summary>
    public Task<Game> CreateGame(string name, string creatorUserId, string orgId);

    /// <summary>
    ///     Find a game by its Id
    /// </summary>
    /// <returns>The game with the given ID, or Null if no game is found</returns>
    public Task<Game?> FindGameById(string id);

    /// <summary>
    ///     Get a game by its Id
    /// </summary>
    /// <returns>The game with the given ID. Throws an exception when no game found</returns>
    public Task<Game> GetGameById(string id);

    /// <summary>
    ///     Find a game from its name
    /// </summary>
    /// <returns>The game with the given name, or Null if no game is found</returns>
    public Task<Game?> FindGameByName(string name);

    /// <summary>
    ///     Get a game by its name
    /// </summary>
    /// <returns>The game with the given name. Throws an exception when no game found</returns>
    public Task<Game> GetGameByName(string name);

    /// <summary>
    ///     Find a player in a game by their global UserId
    /// </summary>
    /// <returns>The player with the given userId, or Null if no player is found</returns>
    public Task<Player?> FindPlayerByUserId(string gameName, string userId);

    /// <summary>
    ///     Find a player in a game by their game-specific ID
    /// </summary>
    /// <returns>The player with the given userId, or Null if no player is found</returns>
    public Task<Player?> FindPlayerByGameId(string gameName, string gameId);

    /// <summary>
    ///     Add a new player to an existing game
    /// </summary>
    public Task<Game> AddPlayer(string gameName, string userId);

    /// <summary>
    ///     Sets isActive for a game
    /// </summary>
    public Task<Game> SetActive(string gameName, bool active, string instigatorId);

    /// <summary>
    ///     Set the <see cref="Player.GameRole" /> of a player
    /// </summary>
    /// <param name="instigatorId">User who is causing the player to change role</param>
    public Task<Game> SetPlayerToRole(string gameName, string userId, Player.GameRole role, string instigatorId);

    /// <summary>
    ///     Log a tag in the specified game
    /// </summary>
    public Task<Game> LogTag(string gameName, string taggerUserId, string tagReceiverGameId);

    /// <summary>
    ///     Get an IEnumerable of games which contain the given user.
    /// </summary>
    /// <param name="limit">Max amount of games to return. Unlimited if not provided</param>
    /// <returns>An IEnumerable of games. May be empty.</returns>
    public Task<List<Game>> GetGamesWithUser(string userId, int? limit = null);

    /// <summary>
    ///     Get an IEnumerable of games which contain the given user and are active.
    /// </summary>
    /// <param name="limit">Max amount of games to return. Unlimited if not provided</param>
    /// <returns>An IEnumerable of games. May be empty.</returns>
    public Task<List<Game>> GetActiveGamesWithUser(string userId, int? limit = null);

    /// <summary>
    ///     Get the event log
    /// </summary>
    public Task<List<GameEventLog>> GetGameEventLog(string gameId);

    /// <summary>
    ///     Event that fires when a new game is created
    /// </summary>
    public event EventHandler<GameUpdatedEventArgs> GameCreated;
    /// <summary>
    ///     Event that fires when a player joins a game
    /// </summary>
    public event EventHandler<PlayerUpdatedEventArgs> PlayerJoinedGame;
    /// <summary>
    ///     Event that fires when a player's role is changed for a game
    /// </summary>
    public event EventHandler<PlayerRoleChangedEventArgs> PlayerRoleChanged;
    /// <summary>
    ///     Event that fires when a tag is logged in a game
    /// </summary>
    public event EventHandler<TagEventArgs> TagLogged;
    /// <summary>
    ///     Event that fires when a game's isActive status is changed
    /// </summary>
    public event EventHandler<GameActiveStatusChangedEventArgs> GameActiveStatusChanged;
}

public class GameUpdatedEventArgs : EventArgs {
    public GameUpdatedEventArgs(Game g, string id)
    {
        Game = g;
        UpdaterId = id;
    }

    public Game Game { get; init; }
    public string UpdaterId { get; init; }
}

public class PlayerUpdatedEventArgs : EventArgs {
    public PlayerUpdatedEventArgs(Game g, Player p)
    {
        Game = g;
        Player = p;
    }

    public Game Game { get; init; }
    public Player Player { get; init; }
}

public class PlayerRoleChangedEventArgs : EventArgs {
    public PlayerRoleChangedEventArgs(Game g, Player p, string instigatorId, Player.GameRole role)
    {
        Game = g;
        Player = p;
        InstigatorId = instigatorId;
        Role = role;
    }

    public Game Game { get; init; }
    public Player Player { get; init; }
    public string InstigatorId { get; init; }
    public Player.GameRole Role { get; init; }
}

public class TagEventArgs : EventArgs {
    public TagEventArgs(Game g, Player tagger, Player tagReceiver)
    {
        Game = g;
        Tagger = tagger;
        TagReceiver = tagReceiver;
    }

    public Game Game { get; init; }
    public Player Tagger { get; init; }
    public Player TagReceiver { get; init; }
}

public class GameActiveStatusChangedEventArgs : EventArgs {
    public readonly bool Active;
    public readonly Game Game;
    public readonly string UpdaterId;

    public GameActiveStatusChangedEventArgs(Game g, string id, bool active)
    {
        Game = g;
        UpdaterId = id;
        Active = active;
    }
}