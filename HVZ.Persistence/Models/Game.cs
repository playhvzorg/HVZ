using NodaTime;
namespace HVZ.Persistence.Models;

public class Game : IdEquatable<Game>
{
    /// <summary>
    /// Unique name of this game.
    /// </summary>
    public string Name { get; init; }
    /// <summary>
    /// Unique identifier of this specific game
    /// </summary>
    public string Id { get; init; }
    /// <summary>
    /// ID of the user who created this game
    /// </summary>
    public string CreatorId { get; init; }
    /// <summary>
    /// ID of the organization this game belongs to
    /// </summary>
    public string OrgId { get; init; }
    /// <summary>
    /// Time that this game was created
    /// </summary>
    public Instant CreatedAt { get; init; }
    /// <summary>
    /// Time that the game was set to Active
    /// </summary>
    public Instant? StartedAt { get; init; }
    /// <summary>
    /// Time that the game was ended
    /// </summary>
    public Instant? EndedAt { get; init; }
    /// <summary>
    /// Current <see cref="GameStatus"/> for the game
    /// </summary>
    public GameStatus Status { get; init; }
    /// <summary>
    /// Weather the game is currently active and tags should be processed
    /// </summary>
    public bool IsActive => Status == GameStatus.Active;
    /// <summary>
    /// Wheter the game is the current and players can register
    /// </summary>
    public bool IsCurrent => Status != GameStatus.Ended;
    /// <summary>
    /// List of events that have happened this game
    /// </summary>
    /// <value></value>
    public List<GameEventLog> EventLog { get; init; }
    /// <summary>
    /// Players who are a human in this game
    /// </summary>
    public HashSet<Player> Humans
    {
        get
        {
            return Players.Where(P => P.Role == Player.gameRole.Human).ToHashSet();
        }
    }
    /// <summary>
    /// Players who are zombies in this game
    /// </summary>
    public HashSet<Player> Zombies
    {
        get
        {
            return Players.Where(P => P.Role == Player.gameRole.Zombie).ToHashSet();
        }
    }
    /// <summary>
    /// Players who are OZs in this game
    /// </summary>
    /// <value></value>
    public HashSet<Player> Ozs
    {
        get
        {
            return Players.Where(P => P.Role == Player.gameRole.Oz).ToHashSet();
        }
    }
    /// <summary>
    /// All Players in this game
    /// </summary>
    /// <value></value>
    public HashSet<Player> Players { get; set; }
    /// <summary>
    /// Players who are Zombies or OZs
    /// </summary>
    /// <value></value>
    public HashSet<Player> ZombiesAndOzs
    {
        get
        {
            return Players.Where(P => P.Role == Player.gameRole.Human || P.Role == Player.gameRole.Oz).ToHashSet();
        }
    }

    /// <summary>
    /// The role to put new people in when they join this game
    /// </summary>
    public Player.gameRole DefaultRole { get; init; }
    /// <summary>
    /// List of game IDs for players interested in being selected as OZs
    /// </summary>
    public HashSet<string> OzPool { get; init; }
    /// <summary>
    /// Optional passcode for joining the OZ pool
    /// </summary>
    public string? OzPassword { get; init; }
    /// <summary>
    /// The maximum number of tags a player can get as an OZ
    /// </summary>
    public int OzMaxTags { get; init; } = 3;

    public Game(string name, string gameid, string creatorid, string orgid, Instant createdat, GameStatus status, Player.gameRole defaultrole, HashSet<Player> players, List<GameEventLog> eventLog)
    {
        Name = name;
        Id = gameid;
        CreatorId = creatorid;
        OrgId = orgid;
        CreatedAt = createdat;
        Status = status;
        DefaultRole = defaultrole;
        Players = players;
        EventLog = eventLog;
        OzPool = ozPool ?? new HashSet<string>();
        OzMaxTags = maxOzTags;
    }

    public override string ToString()
    {
        return $"HVZ.Game@{Name}.{Id}";
    }

    protected override object EqualityId => ToString();

    public enum GameStatus
    {
        New,
        Active,
        Paused,
        Ended
    }
}