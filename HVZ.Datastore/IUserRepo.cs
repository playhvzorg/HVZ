namespace HVZ.Datastore;
using HVZ.Models;
public interface IUserRepo
{
    /// <summary>
    /// Create a new user in the repo
    /// </summary>
    /// <returns>The newly created user</returns>
    public User CreateUser(string firstname, string lastname, string email);
    /// <summary>
    /// Find a player from their ID
    /// </summary>
    /// <returns>The found user or Null if no user found</returns>
    public User? FindById(string id);
    /// <summary>
    /// Find all users with the given first and last name
    /// Null can be passed if only first or last name is known
    /// </summary>
    /// <returns>List of users found or Null if there are none</returns>
    public User[]? FindByName(string? firstname, string? lastname);
}
