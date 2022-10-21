using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using HVZ.Models;
using HVZ.Datastore.MongoDB.Serializers;
using NodaTime;
namespace HVZ.Datastore.MongoDB.Repos;
public class GameRepo : IGameRepo
{
    private const string CollectionName = "Games";
    public readonly IMongoCollection<Game> Collection;
    private readonly IClock _clock;

    static GameRepo()
    {
        BsonClassMap.RegisterClassMap<Game>(cm =>
        {
            cm.MapIdProperty(g => g.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(g => g.UserId);
            cm.MapProperty(g => g.DefaultRole);
            cm.MapProperty(g => g.Humans);
            cm.MapProperty(g => g.Zombies);
            cm.MapProperty(g => g.Ozs);
        });
    }
    public GameRepo(IMongoDatabase database, IClock clock)
    {
        database.CreateCollection(CollectionName);
        Collection = database.GetCollection<Game>(CollectionName);
        _clock = clock;
    }

    public async Task<Game> CreateGame(string userid)
    {
        Game game = new Game(
            id: string.Empty,
            userid: userid,
            createdat: _clock.GetCurrentInstant(),
            state: Game.GameState.registration,
            humans: new HashSet<User>(),
            zombies: new HashSet<User>(),
            ozs: new HashSet<User>()
            );
        await Collection.InsertOneAsync(game);
        return game;
    }

    public async Task<Game?> FindById(string id) =>
        id == "" ? null : await Collection.Find<Game>(g => g.Id == id).FirstOrDefaultAsync();
}
