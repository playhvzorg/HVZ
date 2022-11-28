namespace HVZ.Persistence;
using HVZ.Models;
public interface IOrgRepo
{
    /// <summary>
    /// Create a new organization
    /// </summary>
    public Task<Organization> CreateOrg(string name, string creatorUserId);

    /// <summary>
    /// Find an org by its Id. Returns Null when no org found
    /// </summary>
    public Task<Organization?> FindOrgById(string orgId);

    /// <summary>
    /// Get an org by its Id. Throws an exception if no org found.
    /// </summary>
    public Task<Organization> GetOrgById(string orgId);

    /// <summary>
    /// Find an org by its name. Returns null when no org found.
    /// </summary>
    public Task<Organization?> FindOrgByName(string name);

    /// <summary>
    /// Get an org by its name. Throws an exception if no org found.
    /// </summary>
    public Task<Organization> GetOrgByName(string name);

    /// <summary>
    /// Sets the game that the org is currently playing.
    /// </summary>
    public Task<Organization> SetActiveGameOfOrg(string orgId, string gameId);

    /// <summary>
    /// Find the game that the org is currently playing. Returns null if there is no active game.
    /// </summary>
    public Task<Game?> FindActiveGameOfOrg(string orgId);

    /// <summary>
    /// Get the set of admins for this org
    /// </summary>
    public Task<HashSet<string>> GetAdminsOfOrg(string orgId);

    /// <summary>
    /// Get the set of mods for this org
    /// </summary>
    public Task<HashSet<string>> GetModsOfOrg(string orgId);

    /// <summary>
    /// set the user who owns this org.
    /// </summary>
    public Task<Organization> SetOwner(string orgId, string newOwnerId);

    /// <summary>
    /// Add a user as a moderator of this org.
    /// </summary>
    public Task<Organization> AddModerator(string orgId, string userId);

    /// <summary>
    /// remove a user from this org's moderators.
    /// </summary>
    public Task<Organization> RemoveModerator(string orgId, string userId);

    /// <summary>
    /// Add a user to the admins of this org.
    /// </summary>
    /// <param name="orgId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<Organization> AddAdmin(string orgId, string userId);

    /// <summary>
    /// remove a user from the admins of this org.
    /// </summary>
    public Task<Organization> RemoveAdmin(string orgId, string userId);

}