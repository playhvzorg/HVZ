namespace HVZ.Persistence.MongoDB.Repos;

using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Bson.Serialization.IdGenerators;
using global::MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Models;
using NodaTime;
using Serializers;

public class OrgRepo : IOrgRepo {
    private const string CollectionName = "Orgs";
    private readonly IClock clock;
    public readonly IMongoCollection<Organization> Collection;
    public readonly IGameRepo GameRepo;
    private readonly ILogger logger;
    public readonly IUserRepo UserRepo;

    static OrgRepo()
    {
        BsonClassMap.RegisterClassMap<Organization>(cm => {
            cm.MapIdProperty(o => o.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(o => o.Name);
            cm.MapProperty(o => o.Url);
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
        UserRepo = userRepo;
        GameRepo = gameRepo;
        this.clock = clock;
        this.logger = logger;
        InitIndexes();
    }

    public event EventHandler<OrgUpdatedEventArgs>? AdminsUpdated;
    public event EventHandler<OrgUpdatedEventArgs>? ModsUpdated;

    public async Task<Organization> CreateOrg(string name, string url, string creatorUserId)
    {
        // await _userRepo.GetUserById(creatorUserId);
        Organization org = new(
            id: string.Empty,
            name: name,
            ownerId: creatorUserId,
            moderators: new HashSet<string>(),
            administrators: new HashSet<string> { creatorUserId },
            games: new HashSet<Game>(),
            activeGameId: null,
            createdAt: clock.GetCurrentInstant(),
            url: url
        );
        await Collection.InsertOneAsync(org);
        logger.LogTrace($"New organization {name} created by {creatorUserId}");

        return org;
    }

    public async Task<Game> CreateGame(string name, string creatorId, string orgId)
    {
        if (await IsAdminOfOrg(orgId, creatorId) is false)
            throw new ArgumentException($"User {creatorId} is not an admin of org {orgId} and cannot create a game in this org.");
        if (await FindActiveGameOfOrg(orgId) is not null)
            throw new ArgumentException($"There is already an active game in org {orgId}, not allowing creation of a new game");
        Game game = await GameRepo.CreateGame(name, creatorId, orgId);
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
        return org.ActiveGameId == null ? null : await GameRepo.GetGameById(org.ActiveGameId);
    }

    public async Task<HashSet<string>> GetAdminsOfOrg(string orgId)
    {
        Organization org = await GetOrgById(orgId);
        return org.Administrators;
    }

    public async Task<Organization> AddAdmin(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);
        await UserRepo.GetUserById(userId);//sanity check that the user exists

        org.Administrators.Add(userId);
        logger.LogTrace($"User {userId} added to admin group of org {orgId}");
        OnAdminsUpdated(new OrgUpdatedEventArgs(org));

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
        logger.LogTrace($"User {userId} removed from admin group of org {orgId}");
        OnAdminsUpdated(new OrgUpdatedEventArgs(org));

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
        await UserRepo.GetUserById(userId);//sanity check that the user exists

        org.Moderators.Add(userId);
        logger.LogTrace($"User {userId} added to moderator group of org {orgId}");
        OnModsUpdated(new OrgUpdatedEventArgs(org));

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Moderators, org.Moderators),
            new() { ReturnDocument = ReturnDocument.After }
        );
    }

    public async Task<Organization> RemoveModerator(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);

        org.Moderators.Remove(userId);
        logger.LogTrace($"User {userId} removed from moderator group of org {orgId}");
        OnModsUpdated(new(org));

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Moderators, org.Moderators),
            new() { ReturnDocument = ReturnDocument.After }
        );
    }

    public async Task<bool> IsAdminOfOrg(string orgId, string userId)
    {
        HashSet<string> admins = await GetAdminsOfOrg(orgId);
        return admins.Contains(userId);
    }

    public async Task<bool> IsModOfOrg(string orgId, string userId)
    {
        HashSet<string> mods = await GetModsOfOrg(orgId);
        return mods.Contains(userId);
    }

    public void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Id)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.OwnerId)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Name)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Url))
        });
    }

    protected virtual void OnAdminsUpdated(OrgUpdatedEventArgs o)
    {
        EventHandler<OrgUpdatedEventArgs>? handler = AdminsUpdated;
        handler?.Invoke(this, o);
    }

    protected virtual void OnModsUpdated(OrgUpdatedEventArgs o)
    {
        EventHandler<OrgUpdatedEventArgs>? handler = ModsUpdated;
        handler?.Invoke(this, o);
    }
}