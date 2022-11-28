using NodaTime;
using System.Collections;
namespace HVZ.Models;
public class Organization : IdEquatable<Organization>
{
    /// <summary>
    /// Unique identifier of the org.
    /// </summary>
    public string Id { get; init; }
    protected override object EqualityId => Id;

    /// <summary>
    /// Name of the org.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// ID of the user who owns this org.
    /// </summary>
    public string OwnerId { get; set; }

    /// <summary>
    /// Set of userids who are moderators of this org.
    /// </summary>
    public HashSet<string> Moderators { get; set; }

    /// <summary>
    /// Set of userids who are admins of this org.
    /// </summary>
    public HashSet<string> Administrators { get; set; }

    /// <summary>
    /// Set of gameids owned by this org.
    /// </summary>
    public HashSet<Game> Games { get; set; }

    /// <summary>
    /// The game that is currently active within this org.
    /// </summary>
    public string? ActiveGameId { get; set; }

    /// <summary>
    /// Time this org was created at.
    /// </summary>
    public Instant CreatedAt { get; init; }

    public Organization(string id, string name, string ownerid, HashSet<string> moderators, HashSet<string> administrators, HashSet<Game> games, string? activegameid, Instant createdat)
    {
        Id = id;
        Name = name;
        OwnerId = ownerid;
        Moderators = moderators;
        Administrators = administrators;
        Games = games;
        ActiveGameId = activegameid;
        CreatedAt = createdat;
    }
}