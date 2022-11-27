namespace HVZ.Persistence;
using HVZ.Models;
public interface IUserRepo
{
    /// <summary>
    /// Create a new user in the repo
    /// </summary>
    /// <returns>The newly created user</returns>
    public Task<User> CreateUser(string name, string email);
    /// <summary>
    /// Find a player from their ID
    /// </summary>
    /// <returns>The found user or Null if no user found</returns>
    public Task<User?> FindUserById(string id);
    /// <summary>
    /// Find all users with the given name
    /// </summary>
    /// <returns>List of users found which may be empty</returns>
    public Task<User[]> FindUserByName(string name);

    /// <summary>
    /// Get a user by their ID
    /// </summary>
    /// <returns>The user. Throws ArgumentException when no user found</returns>
    public Task<User> GetUserById(string id);
}
