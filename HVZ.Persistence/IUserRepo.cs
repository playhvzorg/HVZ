namespace HVZ.Persistence;
using HVZ.Persistence.Models;
public interface IUserRepo
{
    /// <summary>
    /// Create a new user in the repo
    /// </summary>
    /// <returns>The newly created user</returns>
    public Task<User> CreateUser(string name, string email);

    /// <summary>
    /// Find a user from their ID
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

    /// <summary>
    /// Find a user from their email
    /// </summary>
    /// <returns>The user or null if no user found</returns>
    public Task<User?> FindUserByEmail(string email);

    /// <summary>
    /// Get a user from their email
    /// </summary>
    /// <returns>the found user. Throws ArgumentException when no user found</returns>
    public Task<User> GetUserByEmail(string email);

    /// <summary>
    /// Delete a user with the given Id.
    /// </summary
    public Task DeleteUser(string id);

    /// <summary>
    /// Change the full name for the speicified user
    /// </summary>
    /// <returns>The updated user</returns>
    public Task<User> SetUserFullName(string id, string fullname);

    /// <summary>
    /// Get the full name of the user
    /// </summary>
    /// <returns>The user's full name</returns>
    public Task<string> GetUserFullName(string id);
}
