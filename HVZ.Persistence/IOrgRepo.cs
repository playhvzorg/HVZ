namespace HVZ.Persistence;
using HVZ.Models;
public interface IOrgRepo
{
    public Task<Organization> CreateOrg(string name, string url, string creatorUserId);

    public Task<Organization?> FindOrgById(string orgId);

    public Task<Organization> GetOrgById(string orgId);

    public Task<Organization?> FindOrgByUrl(string url);

    public Task<Organization> GetOrgByUrl(string url);

    public Task<Organization?> FindOrgByName(string name);

    public Task<Organization> GetOrgByName(string name);

    public Task<Organization> SetActiveGameOfOrg(string orgId, string gameId);

    public Task<Game?> FindActiveGameOfOrg(string orgId);

    public Task<HashSet<string>> GetAdminsOfOrg(string orgId);

    public Task<HashSet<string>> GetModsOfOrg(string orgId);

    public Task<Organization> AssignOwner(string orgId, string newOwnerId);

    public Task<Organization> AddModerator(string orgId, string userId);

    public Task<Organization> RemoveModerator(string orgId, string userId);

    public Task<Organization> AddAdmin(string orgId, string userId);

    public Task<Organization> RemoveAdmin(string orgId, string userId);

}