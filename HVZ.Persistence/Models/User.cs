using NodaTime;
namespace HVZ.Persistence.Models;
public class User : IdEquatable<User>
{
    /// <summary>
    /// User's unique identification
    /// </summary>
    public string Id { get; init; }
    protected override object EqualityId => Id;
    /// <summary>
    /// Given name of the user
    /// </summary>
    public string FullName { get; init; }
    /// <summary>
    /// Email address of the user
    /// </summary>
    public string Email { get; init; }
    /// <summary>
    /// Time this user was created
    /// </summary>
    public Instant CreatedAt { get; init; }

    public User(string id, string fullName, string email)
    {
        Id = id;
        FullName = fullName;
        Email = email;
    }
}