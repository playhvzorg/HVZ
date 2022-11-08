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

    public event EventHandler<GameUpdatedEventArgs>? GameCreated;
    public event EventHandler<GameUpdatedEventArgs>? PlayerJoinedGame;
    public event EventHandler<GameUpdatedEventArgs>? PlayerRoleChanged;
    public event EventHandler<GameUpdatedEventArgs>? TagLogged;
    public event EventHandler<GameUpdatedEventArgs>? GameUpdated;

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
            cm.MapProperty(g => g.Players);
            cm.MapProperty(g => g.IsActive);
        });

        BsonClassMap.RegisterClassMap<Player>(cm =>
        {
            cm.MapProperty(p => p.UserId);
            cm.MapProperty(p => p.GameId);
            cm.MapProperty(p => p.Role);
            cm.MapProperty(p => p.Tags);
            cm.MapProperty(p => p.JoinedGameAt);
            cm.MapProperty(p => p.GameId);
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
        });
    }

    public async Task<Game> CreateGame(string name, string userid)
    {
        Game game = new Game(
            name: name,
            id: string.Empty,
            userid: userid,
            createdat: _clock.GetCurrentInstant(),
            isActive: false,
            defaultrole: Player.gameRole.Human,
            players: new HashSet<Player>()
            );
        await Collection.InsertOneAsync(game);

        GameUpdatedEventArgs gameCreatedEventArgs = new GameUpdatedEventArgs(game);
        OnGameCreated(gameCreatedEventArgs);
        return game;
    }

    public async Task<Game?> FindGameById(string id) =>
        id == "" ? null : await Collection.Find<Game>(g => g.Id == id).FirstOrDefaultAsync();

    public async Task<Game?> FindGameByName(string name) =>
        await Collection.Find<Game>(g => g.Name == name).FirstOrDefaultAsync();

    public async Task<Player?> FindPlayerByUserId(string gameName, string userId)
    {
        Game? game = await FindGameByName(gameName);
        if (game == null)
            throw new ArgumentException($"Game {gameName} not found!");

        Player? player = game.Players.Where(p => p.UserId == userId).FirstOrDefault(defaultValue: null);

        return player;
    }

    public async Task<Player?> FindPlayerByGameId(string gameName, string gameId)
    {
        Game? game = await FindGameByName(gameName);
        if (game == null)
            throw new ArgumentException($"Game {gameName} not found!");

        Player? player = game.Players.Where(p => p.GameId == gameId).FirstOrDefault(defaultValue: null);

        return player;
    }

    public async Task<Game> AddPlayer(string gameName, string userId)
    {
        Game? game = await FindGameByName(gameName);
        if (game == null)
            throw new ArgumentException($"Game {gameName} not found!");
        if (FindPlayerByUserId(gameName, userId).Result != null)
            throw new ArgumentException($"User {userId} is already in Game {gameName}!");

        Player player = new Player(userId, await GenerateGameId(gameName), game.DefaultRole, 0, _clock.GetCurrentInstant());
        HashSet<Player> newPlayers = game.Players;
        newPlayers.Add(player);

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Name == gameName,
            Builders<Game>.Update.Set(g => g.Players, newPlayers),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );
        OnPlayerJoined(new(game));
        return newGame;
    }

    public async Task<Game> SetActive(string gameName, bool active)
    {
        Game? game = await FindGameByName(gameName);
        if (game == null)
            throw new ArgumentException($"Game {gameName} not found!");

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Name == gameName,
            Builders<Game>.Update.Set(g => g.IsActive, active),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );
        OnGameUpdated(new(game));
        return newGame;
    }

    public async Task<Game> SetPlayerToRole(string gameName, string userId, Player.gameRole role)
    {
        Game? game = await FindGameByName(gameName);
        if (game == null)
            throw new ArgumentException($"Game {gameName} not found!");

        //TODO: this surely can be optimized
        Player? player = await FindPlayerByUserId(gameName, userId);
        if (player == null)
            throw new ArgumentException($"User {userId} not found in game {gameName}");

        var players = game.Players;
        players.Where(p => p.UserId == userId).First().Role = role;

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Name == gameName,
            Builders<Game>.Update.Set(g => g.Players, players),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );
        OnPlayerRoleChanged(new(game));
        return newGame;
    }

    public async Task<Game> LogTag(string gameName, string taggerUserId, string tagRecieverGameId)
    {
        if (taggerUserId == tagRecieverGameId)
            throw new ArgumentException("userIds are equal, players cannot tag themselves");

        Game? game = await FindGameByName(gameName);
        if (game == null)
            throw new ArgumentException($"Game {gameName} not found!");

        if (!game.IsActive)
            throw new ArgumentException($"Game {gameName} is not active!");

        if (game.Players.Where(p => p.UserId == taggerUserId).Count() == 0)
            throw new ArgumentException($"Player {taggerUserId} not found in Game {gameName}!");

        if (game.Players.Where(p => p.GameId == tagRecieverGameId).Count() == 0)
            throw new ArgumentException($"Player {tagRecieverGameId} not found in Game {gameName}!");

        var Players = game.Players;
        Player.gameRole taggerRole = Players.Where(p => p.UserId == taggerUserId).First().Role;
        if (taggerRole != Player.gameRole.Zombie && taggerRole != Player.gameRole.Oz)
            throw new ArgumentException($"Tagger {taggerUserId} is not a zombie or OZ!");

        Player.gameRole tagRecieverRole = Players.Where(p => p.GameId == tagRecieverGameId).First().Role;
        if (tagRecieverRole != Player.gameRole.Human)
            throw new ArgumentException($"tagReciever {tagRecieverGameId} is not Human!");

        Players.Where(p => p.UserId == taggerUserId).First().Tags += 1;
        Players.Where(p => p.GameId == tagRecieverGameId).First().Role = Player.gameRole.Zombie;

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Name == gameName,
            Builders<Game>.Update.Set(g => g.Players, Players),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );

        OnTag(new(game));
        return newGame;
    }

    private async Task<String> GenerateGameId(string gameName)
    {
        Game? game = await FindGameByName(gameName);
        if (game == null)
            throw new ArgumentException($"Game {gameName} not found!");

        int id;
        do
        {
            id = Random.Shared.Next(1000, 9999);
        } while (game.Players.Where(p => p.GameId == id.ToString()).Count() > 0);
        return id.ToString();
    }
    protected virtual void OnGameCreated(GameUpdatedEventArgs g)
    {
        EventHandler<GameUpdatedEventArgs>? handler = GameCreated;
        if (handler != null)
        {
            handler(this, g);
        }
    }
    protected virtual void OnPlayerJoined(GameUpdatedEventArgs g)
    {
        EventHandler<GameUpdatedEventArgs>? handler = PlayerJoinedGame;
        if (handler != null)
        {
            handler(this, g);
        }
    }
    protected virtual void OnPlayerRoleChanged(GameUpdatedEventArgs g)
    {
        EventHandler<GameUpdatedEventArgs>? handler = PlayerRoleChanged;
        if (handler != null)
        {
            handler(this, g);
        }
    }
    protected virtual void OnGameUpdated(GameUpdatedEventArgs g)
    {
        EventHandler<GameUpdatedEventArgs>? handler = GameUpdated;
        if (handler != null)
        {
            handler(this, g);
        }
    }
    protected virtual void OnTag(GameUpdatedEventArgs g)
    {
        EventHandler<GameUpdatedEventArgs>? handler = TagLogged;
        if (handler != null)
        {
            handler(this, g);
        }
    }
}