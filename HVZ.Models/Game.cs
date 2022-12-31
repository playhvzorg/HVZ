using System.Collections.Generic;
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

    public Game(string name, string gameid, string creatorid, string orgid, Instant createdat, Boolean isActive, Player.gameRole defaultrole, HashSet<Player> players)
    {
        Name = name;
        Id = gameid;
        CreatorId = creatorid;
        OrgId = orgid;
        CreatedAt = createdat;
        IsActive = isActive;
        DefaultRole = defaultrole;
        Players = players;
    }

    public override string ToString()
    {
        return $"HVZ.Game@{Name}.{Id}";
    }

    protected override object EqualityId => ToString();
}