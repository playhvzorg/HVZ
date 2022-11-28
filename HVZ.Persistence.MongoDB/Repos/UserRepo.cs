using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using HVZ.Models;
using HVZ.Persistence.MongoDB.Serializers;
using NodaTime;
namespace HVZ.Persistence.MongoDB.Repos;

public class UserRepo : IUserRepo
{
    private const string CollectionName = "Users";
    public readonly IMongoCollection<User> Collection;
    private readonly IClock _clock;

    static UserRepo()
    {
        BsonClassMap.RegisterClassMap<User>(cm =>
        {
            cm.MapIdProperty(u => u.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(u => u.Name);
            cm.MapProperty(u => u.Email);
            cm.MapProperty(u => u.CreatedAt);
        });
    }

    public UserRepo(IMongoDatabase database, IClock clock)
    {
        var filter = new BsonDocument("name", CollectionName);
        var collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
        if (!collections.Any())
            database.CreateCollection(CollectionName);
        Collection = database.GetCollection<User>(CollectionName);
        _clock = clock;
        InitIndexes();
    }

    public void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Id)),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Name)),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Email))
        });
    }

    public async Task<User> CreateUser(string name, string email)
    {
        //Ensure email is Unique
        if (await Collection.Find(u => u.Email == email).AnyAsync())
            throw new ArgumentException($"There is already a user registered with the email {email}");

        User user = new(
            id: string.Empty,
            name: name,
            email: email
        );

        await Collection.InsertOneAsync(user);
        return user;
    }

    public async Task<User?> FindUserById(string userId)
        => userId == string.Empty ? null : await Collection.Find(u => u.Id == userId).FirstAsync();

    public async Task<User[]> FindUserByName(string name)
    {
        if (name == string.Empty)
            throw new ArgumentException("Name must not be empty");

        var users = await Collection.Find(u => u.Name.ToLower().Contains(name.ToLower())).ToListAsync();
        return users.ToArray();
    }

    public async Task<User> GetUserById(string userId)
    {
        User? user = await FindUserById(userId);
        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found!");
        return (User)user;
    }


}