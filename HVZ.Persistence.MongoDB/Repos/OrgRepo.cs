using HVZ.Persistence.Models;
using HVZ.Persistence.MongoDB.Serializers;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;

namespace HVZ.Persistence.MongoDB.Repos;
public class OrgRepo : IOrgRepo
{
    private const string CollectionName = "Orgs";
    public readonly IMongoCollection<Organization> Collection;
    public readonly IUserRepo _userRepo;
    public readonly IGameRepo _gameRepo;
    private readonly IClock _clock;
    private readonly ILogger _logger;

    public event EventHandler<OrgUpdatedEventArgs>? AdminsUpdated;
    public event EventHandler<OrgUpdatedEventArgs>? ModsUpdated;

    static OrgRepo()
    {
        BsonClassMap.RegisterClassMap<Organization>(cm =>
        {
            cm.MapIdProperty(o => o.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(o => o.Name);
            cm.MapProperty(o => o.Url);
            cm.MapProperty(o => o.Description);
            cm.MapProperty(o => o.OwnerId);
            cm.MapProperty(o => o.Moderators);
            cm.MapProperty(o => o.Administrators);
            cm.MapProperty(o => o.Games);
            cm.MapProperty(o => o.ActiveGameId);
            cm.MapProperty(o => o.CreatedAt)
                .SetSerializer(InstantSerializer.Instance);
        });
    }

    public OrgRepo(IMongoDatabase database, IClock clock, IUserRepo userRepo, IGameRepo gameRepo, ILogger logger)
    {
        var filter = new BsonDocument("name", CollectionName);
        var collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
        if (!collections.Any())
            database.CreateCollection(CollectionName);
        Collection = database.GetCollection<Organization>(CollectionName);
        _userRepo = userRepo;
        _gameRepo = gameRepo;
        _clock = clock;
        _logger = logger;
        InitIndexes();
    }

    public void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Id)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.OwnerId)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Name)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Url)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Description))
        });
    }

    public async Task<Organization> CreateOrg(string name, string url, string creatorUserId)
    {
        // await _userRepo.GetUserById(creatorUserId);
        Organization org = new(
            id: string.Empty,
            name: name,
            ownerid: creatorUserId,
            moderators: new(),
            administrators: new() { creatorUserId },
            games: new(),
            activegameid: null,
            createdat: _clock.GetCurrentInstant(),
            url: url
        );
        await Collection.InsertOneAsync(org);
        _logger.LogTrace($"New organization {name} created by {creatorUserId}");

        return org;
    }

    public async Task<Game> CreateGame(string name, string creatorId, string orgId)
    {
        if (await IsAdminOfOrg(orgId, creatorId) is false)
            throw new ArgumentException($"User {creatorId} is not an admin of org {orgId} and cannot create a game in this org.");
        if (await FindActiveGameOfOrg(orgId) is not null)
            throw new ArgumentException($"There is already an active game in org {orgId}, not allowing creation of a new game");
        Game game = await _gameRepo.CreateGame(name, creatorId, orgId);
        await SetActiveGameOfOrg(orgId, game.Id);
        return game;
    }

    public async Task<Organization?> FindOrgById(string orgId)
        => orgId == string.Empty ? null : await Collection.Find<Organization>(o => o.Id == orgId).FirstOrDefaultAsync();

    public async Task<Organization> GetOrgById(string orgId)
    {
        Organization? org = await FindOrgById(orgId);
        if (org == null)
            throw new ArgumentException($"Org with id {orgId} not found!");
        return (Organization)org;
    }

    public async Task<Organization?> FindOrgByUrl(string url)
        => url == string.Empty ? null : await Collection.Find<Organization>(o => o.Url == url).FirstOrDefaultAsync();

    public async Task<Organization> GetOrgByUrl(string url)
    {
        Organization? org = await FindOrgByUrl(url);
        if (org == null)
            throw new ArgumentException($"Org with url {url} not found!");
        return (Organization)org;
    }

    public async Task<Organization?> FindOrgByName(string name)
        => name == string.Empty ? null : await Collection.Find<Organization>(o => o.Name == name).FirstOrDefaultAsync();

    public Task<Organization> GetOrgByName(string name)
    {
        throw new NotImplementedException();
    }

    public async Task<Organization> SetActiveGameOfOrg(string orgId, string gameId)
    {
        Organization org = await GetOrgById(orgId);
        if (org.ActiveGameId == gameId)
            return org;
        else
            return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.ActiveGameId, gameId),
            new() { ReturnDocument = ReturnDocument.After });
    }

    public async Task<Game?> FindActiveGameOfOrg(string orgId)
    {
        Organization org = await GetOrgById(orgId);
        return org.ActiveGameId == null ? null : await _gameRepo.GetGameById(org.ActiveGameId);
    }

    public async Task<HashSet<string>> GetAdminsOfOrg(string orgId)
    {
        Organization org = await GetOrgById(orgId);
        return org.Administrators;
    }

    public async Task<Organization> AddAdmin(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);
        await _userRepo.GetUserById(userId); //sanity check that the user exists

        org.Administrators.Add(userId);
        _logger.LogTrace($"User {userId} added to admin group of org {orgId}");
        OnAdminsUpdated(new(org));

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Administrators, org.Administrators),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<Organization> RemoveAdmin(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);
        if (org.OwnerId == userId)
            throw new ArgumentException($"User with ID {userId} is the owner of org with id {org.Id}, cannot remove them from this org's admins.");

        org.Administrators.Remove(userId);
        _logger.LogTrace($"User {userId} removed from admin group of org {orgId}");
        OnAdminsUpdated(new(org));

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Administrators, org.Administrators),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<HashSet<string>> GetModsOfOrg(string orgId)
    {
        Organization org = await GetOrgById(orgId);
        return org.Moderators;
    }

    public async Task<Organization> SetOwner(string orgId, string newOwnerId)
    {
        Organization org = await GetOrgById(orgId);
        if (!org.Administrators.Contains(newOwnerId))
            throw new ArgumentException($"user {newOwnerId} is not an org administrator in org {orgId}");
        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.OwnerId, newOwnerId),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<Organization> AddModerator(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);
        await _userRepo.GetUserById(userId); //sanity check that the user exists

        org.Moderators.Add(userId);
        _logger.LogTrace($"User {userId} added to moderator group of org {orgId}");
        OnModsUpdated(new(org));

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Moderators, org.Moderators),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<Organization> RemoveModerator(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);

        org.Moderators.Remove(userId);
        _logger.LogTrace($"User {userId} removed from moderator group of org {orgId}");
        OnModsUpdated(new(org));

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Moderators, org.Moderators),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<bool> IsAdminOfOrg(string orgId, string userId)
    {
        var admins = await GetAdminsOfOrg(orgId);
        return admins.Contains(userId);
    }

    public async Task<bool> IsModOfOrg(string orgId, string userId)
    {
        var mods = await GetModsOfOrg(orgId);
        return mods.Contains(userId);
    }

    public async Task<Organization> SetOrgDescription(string orgId, string description)
    {
        _logger.LogTrace($"Description for org {orgId} has been changed to {description}");

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Description, description),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<string> GetOrgDescription(string orgId)
    {
        var org = await GetOrgById(orgId);
        return org?.Description ?? string.Empty;
    }

    protected virtual void OnAdminsUpdated(OrgUpdatedEventArgs o)
    {
        EventHandler<OrgUpdatedEventArgs>? handler = AdminsUpdated;
        if (handler != null)
        {
            handler(this, o);
        }
    }

    protected virtual void OnModsUpdated(OrgUpdatedEventArgs o)
    {
        EventHandler<OrgUpdatedEventArgs>? handler = ModsUpdated;
        if (handler != null)
        {
            handler(this, o);
        }
    }
}