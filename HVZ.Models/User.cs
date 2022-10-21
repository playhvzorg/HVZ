using NodaTime;
namespace HVZ.Models;
public class User
{
    public string Id { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Email { get; init; }
    public Instant CreatedAt { get; init; }

    public User(string id, string firstname, string lastname, string email)
    {
        Id = id;
        FirstName = firstname;
        LastName = lastname;
        Email = email;
    }
}