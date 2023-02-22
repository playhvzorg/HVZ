namespace HVZ.Models;

using NodaTime;

public class Organization : IdEquatable<Organization> {

    public Organization(string id, string name, string ownerId, HashSet<string> moderators, HashSet<string> administrators, HashSet<Game> games, string? activeGameId, Instant createdAt, string url)
    {
        Id = id;
        Name = name;
        OwnerId = ownerId;
        Moderators = moderators;
        Administrators = administrators;
        Games = games;
        ActiveGameId = activeGameId;
        CreatedAt = createdAt;
        Url = url;
    }

    /// <summary>
    ///     Unique identifier of the org.
    /// </summary>
    public string Id { get; init; }
    protected override object EqualityId => Id;

    /// <summary>
    ///     URL parameter of the org.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    ///     Name of the org.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     ID of the user who owns this org.
    /// </summary>
    public string OwnerId { get; set; }

    /// <summary>
    ///     Set of userids who are moderators of this org.
    /// </summary>
    public HashSet<string> Moderators { get; set; }

    /// <summary>
    ///     Set of userids who are admins of this org.
    /// </summary>
    public HashSet<string> Administrators { get; set; }

    /// <summary>
    ///     Set of gameids owned by this org.
    /// </summary>
    public HashSet<Game> Games { get; set; }

    /// <summary>
    ///     The game that is currently active within this org.
    /// </summary>
    public string? ActiveGameId { get; set; }

    /// <summary>
    ///     Time this org was created at.
    /// </summary>
    public Instant CreatedAt { get; init; }
}