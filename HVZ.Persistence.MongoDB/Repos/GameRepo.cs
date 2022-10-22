using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using HVZ.Models;
using HVZ.Persistence.MongoDB.Serializers;
using NodaTime;
namespace HVZ.Persistence.MongoDB.Repos;
public class GameRepo : IGameRepo
{
    private const string CollectionName = "Games";
    public readonly IMongoCollection<Game> Collection;
    private readonly IClock _clock;

    /// <summary>
    /// This event fires when a new game is added to the repo
    /// </summary>
    public event EventHandler<GameCreatedEventArgs>? GameCreated;

    static GameRepo()
    {
        BsonClassMap.RegisterClassMap<Game>(cm =>
        {
            cm.MapProperty(g => g.Name);
            cm.MapIdProperty(g => g.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(g => g.UserId);
            cm.MapProperty(g => g.CreatedAt);
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
        InitIndexes();
    }

    public void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<Game>(Builders<Game>.IndexKeys.Ascending(g => g.CreatedAt)),
            new CreateIndexModel<Game>(Builders<Game>.IndexKeys.Ascending(g => g.Name)),
            new CreateIndexModel<Game>(Builders<Game>.IndexKeys.Ascending(g => g.Id)),
        }

        );
    }

    public async Task<Game> CreateGame(string name, string userid)
    {
        Game game = new Game(
            name: name,
            id: string.Empty,
            userid: userid,
            createdat: _clock.GetCurrentInstant(),
            state: Game.GameState.Inactive,
            defaultrole: Game.DefaultPlayerRole.Human,
            humans: new HashSet<User>(),
            zombies: new HashSet<User>(),
            ozs: new HashSet<User>()
            );
        await Collection.InsertOneAsync(game);

        GameCreatedEventArgs gameCreatedEventArgs = new GameCreatedEventArgs(game);
        OnGameCreated(gameCreatedEventArgs);
        return game;
    }

    public async Task<Game?> FindById(string id) =>
        id == "" ? null : await Collection.Find<Game>(g => g.Id == id).FirstOrDefaultAsync();

    public async Task<Game?> FindByName(string name) =>
        await Collection.Find<Game>(g => g.Name == name).FirstOrDefaultAsync();


    protected virtual void OnGameCreated(GameCreatedEventArgs g)
    {
        EventHandler<GameCreatedEventArgs>? handler = GameCreated;
        if (handler != null)
        {
            handler(this, g);
        }
    }
}