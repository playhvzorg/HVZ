namespace HVZ.Persistence;
using HVZ.Persistence.Models;
public interface IOrgRepo
{
    public Task<Organization> CreateOrg(string name, string url, string creatorUserId);

    /// <summary>
    /// Find an org by its Id. Returns Null when no org found.
    /// </summary>
    public Task<Organization?> FindOrgById(string orgId);

    /// <summary>
    /// Get an org by its Id. Throws an exception if no org found.
    /// </summary>
    public Task<Organization> GetOrgById(string orgId);

    /// <summary>
    /// Get an org by its Url Parameter. Returns Null when no org found.
    /// </summary>
    public Task<Organization?> FindOrgByUrl(string url);

    /// <summary>
    /// Get an org by its Url Parameter. Throws an exception if no org found.
    /// </summary>
    public Task<Organization> GetOrgByUrl(string url);

    /// <summary>
    /// Get an org by its Name. Returns Null when no org found.
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
    /// Creates a game that belongs to an org.
    /// </summary>
    public Task<Game> CreateGame(string name, string orgId, string creatorId);
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

    /// <summary>
    /// Check if the user is an admin for the given org
    /// </summary>
    /// <param name="orgId"></param>
    /// <param name="userId"></param>
    /// <returns>Whether the user is an admin</returns>
    public Task<bool> IsAdminOfOrg(string orgId, string userId);

    /// <summary>
    /// Check if the user is an moderator for the given org
    /// </summary>
    /// <param name="orgId"></param>
    /// <param name="userId"></param>
    /// <returns>Whether the user is an moderator</returns>
    public Task<bool> IsModOfOrg(string orgId, string userId);

    /// <summary>
    /// Save the description property for the given org.
    /// </summary>
    /// <param name="orgId"></param>
    /// <param name="description"></param>
    public Task<Organization> SetOrgDescription(string orgId, string description);

    /// <summary>
    /// Event that fires whenever someone is added to or removed from an org's Administrators
    /// </summary>
    public event EventHandler<OrgUpdatedEventArgs> AdminsUpdated;

    /// <summary>
    /// Event that fires whenever someone is added to or removed from an org's Moderators
    /// </summary>
    public event EventHandler<OrgUpdatedEventArgs> ModsUpdated;
}

public class OrgUpdatedEventArgs : EventArgs
{
    public Organization Org;

    public OrgUpdatedEventArgs(Organization org)
    {
        Org = org;
    }
}