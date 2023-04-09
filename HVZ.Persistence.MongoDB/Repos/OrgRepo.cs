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
    public event EventHandler<OrgUpdatedEventArgs>? SettingsUpdated;

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
            cm.MapProperty(o => o.RequireProfilePictureForPlayer);
            cm.MapProperty(o => o.RequireVerifiedEmailForPlayer);
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
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Description)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.RequireProfilePictureForPlayer)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.RequireVerifiedEmailForPlayer))
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

    public async Task<Game> CreateGame(string name, string creatorId, string orgId, int ozTagCount = 3)
    {
        if (await IsAdminOfOrg(orgId, creatorId) is false)
            throw new ArgumentException(
                $"User {creatorId} is not an admin of org {orgId} and cannot create a game in this org.");
        if (await FindActiveGameOfOrg(orgId) is not null)
            throw new ArgumentException($"There is already an active game in org {orgId}, not allowing creation of a new game");
        Game game = await _gameRepo.CreateGame(name, creatorId, orgId, ozTagCount);
        await SetActiveGameOfOrg(orgId, game.Id);
        return game;
    }

    public async Task<Game> EndGame(string orgId, string instigatorId)
    {
        Organization org = await GetOrgById(orgId);
        if (org.ActiveGameId is null)
            throw new ArgumentException($"There is no active game in {orgId}");
        if (await IsAdminOfOrg(orgId, instigatorId) is false)
            throw new ArgumentException($"User {instigatorId} is not an admin of org {orgId} and cannot end the game in this org.");

        Game game = await _gameRepo.EndGame(org.ActiveGameId, instigatorId);
        await RemoveActiveGameOfOrg(orgId);
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

    public async Task<Organization> RemoveActiveGameOfOrg(string orgId)
    {
        Organization org = await GetOrgById(orgId);
        if (org.ActiveGameId is null)
            return org;

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.ActiveGameId, null),
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
            throw new ArgumentException(
                $"User with ID {userId} is the owner of org with id {org.Id}, cannot remove them from this org's admins.");

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

    public async Task<Organization> SetOrgName(string orgId, string name)
    {
        var org = await GetOrgById(orgId);

        _logger.LogTrace($"Name for org {orgId} has been changed to {name}");

        OnSettingsUpdated(new(org));

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Name, name),
            new() { ReturnDocument = ReturnDocument.After }
        );
    }

    public async Task<string> GetOrgName(string orgId)
        => (await GetOrgById(orgId)).Name;

    public async Task<Organization> SetOrgDescription(string orgId, string description)
    {
        var org = await GetOrgById(orgId);

        _logger.LogTrace($"Description for org {orgId} has been changed to {description}");

        OnSettingsUpdated(new(org));

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Description, description),
            new() { ReturnDocument = ReturnDocument.After }
        );
    }

    public async Task<string> GetOrgDescription(string orgId)
        => (await GetOrgById(orgId)).Description;

    public async Task<Organization> SetRequireVerifiedEmail(string orgId, bool requireVerifiedEmail)
    {
        var org = await GetOrgById(orgId);

        _logger.LogTrace($"Require Verified Email for org {orgId} has been changed to {requireVerifiedEmail}");

        OnSettingsUpdated(new(org));


        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.RequireVerifiedEmailForPlayer, requireVerifiedEmail),
            new() { ReturnDocument = ReturnDocument.After }
        );
    }

    public async Task<bool> GetRequireVerifiedEmail(string orgId)
        => (await GetOrgById(orgId)).RequireVerifiedEmailForPlayer;

    public async Task<Organization> SetRequireProfilePicture(string orgId, bool requireProfilePicture)
    {
        var org = await GetOrgById(orgId);

        _logger.LogTrace($"Require Profile Picture for org {orgId} has been changed to {requireProfilePicture}");

        OnSettingsUpdated(new(org));

        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.RequireProfilePictureForPlayer, requireProfilePicture),
            new() { ReturnDocument = ReturnDocument.After }
        );
    }

    public async Task<bool> GetRequireProfilePicture(string orgId)
        => (await GetOrgById(orgId)).RequireProfilePictureForPlayer;

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

    protected virtual void OnSettingsUpdated(OrgUpdatedEventArgs o)
    {
        EventHandler<OrgUpdatedEventArgs>? handler = SettingsUpdated;
        if (handler != null)
        {
            handler(this, o);
        }
    }
}