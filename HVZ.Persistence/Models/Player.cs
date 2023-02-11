using NodaTime;
namespace HVZ.Persistence.Models;

/// <summary>
/// Represents all of the info about a user in a specific game.
/// One User may be a Player in multiple different Games.
/// </summary>
public class Player : IdEquatable<Player>
{
    /// <summary>
    /// The userid of the Player, matching to their User
    /// </summary>
    public string UserId { get; init; }

    /// <summary>
    /// Id of the player in a specific game
    /// </summary>
    public string GameId { get; set; }

    protected override object EqualityId => UserId + "@" + GameId;

    public enum gameRole
    {
        Human,
        Zombie,
        Oz
    }

    /// <summary>
    /// The role of the player in this game. See <see cref="gameRole"/>
    /// </summary>
    public gameRole Role { get; set; }

    /// <summary>
    /// The amount of tags this player has goten this game
    /// </summary>
    /// <value></value>
    public int Tags { get; set; }

    /// <summary>
    /// The time this user joined this game
    /// </summary>
    public Instant JoinedGameAt { get; init; }

    public Player(string userid, string gameId, gameRole role, int tags, Instant joinedGameAt)
    {
        UserId = userid;
        GameId = gameId;
        Role = role;
        Tags = tags;
        JoinedGameAt = joinedGameAt;
    }
}