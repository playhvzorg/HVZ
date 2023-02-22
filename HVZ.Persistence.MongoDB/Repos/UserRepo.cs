namespace HVZ.Persistence.MongoDB.Repos;

using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Bson.Serialization.IdGenerators;
using global::MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Models;
using NodaTime;
using Serializers;

public class UserRepo : IUserRepo {
    private const string CollectionName = "Users";
    private readonly IClock clock;
    public readonly IMongoCollection<User> Collection;
    private readonly ILogger logger;

    static UserRepo()
    {
        BsonClassMap.RegisterClassMap<User>(cm => {
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
        IAsyncCursor<BsonDocument>? collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
        if (!collections.Any())
            database.CreateCollection(CollectionName);
        Collection = database.GetCollection<User>(CollectionName);
        this.clock = clock;
        this.logger = logger;
        InitIndexes();
    }

    public async Task<User> CreateUser(string name, string email)
    {
        //Ensure email is Unique
        if (await Collection.Find(u => u.Email == email).AnyAsync())
            throw new ArgumentException($"There is already a user registered with the email {email}");

        // Ensure that there are no leading or trailing whitespace characters
        name = name.Trim();

        User user = new(
            string.Empty,
            name,
            email
        );
        logger.LogTrace($"Creating new user: name: {name} | email: {email}");
        await Collection.InsertOneAsync(user);
        return user;
    }

    public async Task<User?> FindUserById(string userId)
    {
        return userId == string.Empty ? null : await Collection.Find(u => u.Id == userId).FirstOrDefaultAsync();
    }

    public async Task<User[]> FindUserByName(string name)
    {
        if (name == string.Empty)
            throw new ArgumentException("Name must not be empty");

        List<User>? users = await Collection.Find(u => u.FullName.ToLower().Contains(name.ToLower())).ToListAsync();
        return users.ToArray();
    }

    public async Task<User> GetUserById(string userId)
    {
        User? user = await FindUserById(userId);
        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found!");
        return user;
    }

    public async Task<User?> FindUserByEmail(string email)
    {
        return email == string.Empty ? null : await Collection.Find(u => u.Email.ToLowerInvariant() == email.ToLowerInvariant()).FirstOrDefaultAsync();
    }

    public async Task<User> GetUserByEmail(string email)
    {
        User? user = await FindUserByEmail(email);
        if (user == null)
            throw new ArgumentException($"User with email {email} not found!");
        return user;
    }

    public async Task DeleteUser(string userId)
    {
        logger.LogTrace($"Deleting user {userId}");
        await Collection.FindOneAndDeleteAsync(u => u.Id == userId);
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
}