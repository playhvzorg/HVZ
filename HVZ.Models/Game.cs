using NodaTime;
namespace HVZ.Models;

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
    /// Weather the game is currently active and tags should be processed
    /// </summary>
    public Boolean IsActive { get; init; }
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
            return Players.Where(p => p.Role == Player.GameRole.Human).ToHashSet();
        }
    }
    /// <summary>
    /// Players who are zombies in this game
    /// </summary>
    public HashSet<Player> Zombies
    {
        get
        {
            return Players.Where(p => p.Role == Player.GameRole.Zombie).ToHashSet();
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
            return Players.Where(p => p.Role == Player.GameRole.Oz).ToHashSet();
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
            return Players.Where(p => p.Role == Player.GameRole.Human || p.Role == Player.GameRole.Oz).ToHashSet();
        }
    }

    /// <summary>
    /// The role to put new people in when they join this game
    /// </summary>
    public Player.GameRole DefaultRole { get; init; }

    public Game(string name, string gameId, string creatorId, string orgId, Instant createdAt, bool isActive, Player.GameRole defaultRole, HashSet<Player> players, List<GameEventLog> eventLog)
    {
        Name = name;
        Id = gameId;
        CreatorId = creatorId;
        OrgId = orgId;
        CreatedAt = createdAt;
        IsActive = isActive;
        DefaultRole = defaultRole;
        Players = players;
        EventLog = eventLog;
    }

    public override string ToString()
    {
        return $"HVZ.Game@{Name}.{Id}";
    }

    protected override object EqualityId => ToString();
}