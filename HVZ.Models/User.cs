using NodaTime;
namespace HVZ.Models;
public class User
{
    /// <summary>
    /// User's unique identification
    /// </summary>
    public string Id { get; init; }
    /// <summary>
    /// Legal first name of the user
    /// </summary>
    public string FirstName { get; init; }
    /// <summary>
    /// Legal last name of the user
    /// </summary>
    public string LastName { get; init; }
    /// <summary>
    /// Email address of the user
    /// </summary>
    public string Email { get; init; }
    /// <summary>
    /// Time this user was created
    /// </summary>
    public Instant CreatedAt { get; init; }

    public User(string id, string firstname, string lastname, string email)
    {
        Id = id;
        FirstName = firstname;
        LastName = lastname;
        Email = email;
    }
}