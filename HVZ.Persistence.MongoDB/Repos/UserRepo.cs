using HVZ.Persistence.Models;
using HVZ.Persistence.MongoDB.Serializers;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using Microsoft.Extensions.Logging;
namespace HVZ.Persistence.MongoDB.Repos;

public class UserRepo : IUserRepo
{
    private const string CollectionName = "Users";
    public readonly IMongoCollection<User> Collection;
    private readonly IClock _clock;
    private readonly ILogger _logger;

    static UserRepo()
    {
        BsonClassMap.RegisterClassMap<User>(cm =>
        {
            cm.MapIdProperty(u => u.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(u => u.FullName);
            cm.MapProperty(u => u.Email);
            cm.MapProperty(u => u.CreatedAt)
                .SetSerializer(InstantSerializer.Instance);
        });
    }

    public UserRepo(IMongoDatabase database, IClock clock, ILogger logger)
    {
        var filter = new BsonDocument("name", CollectionName);
        var collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
        if (!collections.Any())
            database.CreateCollection(CollectionName);
        Collection = database.GetCollection<User>(CollectionName);
        _clock = clock;
        _logger = logger;
        InitIndexes();
    }

    public void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Id)),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.FullName)),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Email))
        });
    }

    public async Task<User> CreateUser(string name, string email)
    {
        //Ensure email is Unique
        if (await Collection.Find(u => u.Email == email).AnyAsync())
            throw new ArgumentException($"There is already a user registered with the email {email}");

        // Ensure that there are no leading or trailing whitespace characters
        name = name.Trim();

        User user = new(
            id: string.Empty,
            fullName: name,
            email: email
        );
        _logger.LogTrace($"Creating new user: name: {name} | email: {email}");
        await Collection.InsertOneAsync(user);
        return user;
    }

    public async Task<User?> FindUserById(string userId)
        => userId == string.Empty ? null : await Collection.Find(u => u.Id == userId).FirstOrDefaultAsync();

    public async Task<User[]> FindUserByName(string name)
    {
        if (name == string.Empty)
            throw new ArgumentException("Name must not be empty");

        var users = await Collection.Find(u => u.FullName.ToLower().Contains(name.ToLower())).ToListAsync();
        return users.ToArray();
    }

    public async Task<User> GetUserById(string userId)
    {
        User? user = await FindUserById(userId);
        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found!");
        return (User)user;
    }

    public async Task<User?> FindUserByEmail(string email)
        => email == string.Empty
            ? null
            : await Collection.Find(u => u.Email.ToLowerInvariant() == email.ToLowerInvariant()).FirstOrDefaultAsync();

    public async Task<User> GetUserByEmail(string email)
    {
        User? user = await FindUserByEmail(email);
        if (user == null)
            throw new ArgumentException($"User with email {email} not found!");
        return (User)user;
    }

    public async Task DeleteUser(string userId)
    {
        _logger.LogTrace($"Deleting user {userId}");
        await Collection.FindOneAndDeleteAsync(u => u.Id == userId);
    }
    public async Task<User> SetUserFullName(string userId, string fullname)
    {
        var user = await GetUserById(userId);

        _logger.LogTrace($"User {userId} changed their name from {user.FullName} to {fullname}");

        return await Collection.FindOneAndUpdateAsync(u => u.Id == userId,
            Builders<User>.Update.Set(u => u.FullName, fullname),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<string> GetUserFullName(string userId)
    {
        var user = await GetUserById(userId);
        return user.FullName;
    }
}