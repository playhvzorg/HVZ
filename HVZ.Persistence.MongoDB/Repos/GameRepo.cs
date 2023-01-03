using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using HVZ.Models;
using HVZ.Persistence.MongoDB.Serializers;
using NodaTime;
using Microsoft.Extensions.Logging;

namespace HVZ.Persistence.MongoDB.Repos;
public class GameRepo : IGameRepo
{
    private const string CollectionName = "Games";
    public readonly IMongoCollection<Game> Collection;
    private readonly IClock _clock;
    private readonly ILogger _logger;

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
            cm.MapProperty(g => g.CreatorId);
            cm.MapProperty(g => g.OrgId);
            cm.MapProperty(g => g.CreatedAt)
                .SetSerializer(InstantSerializer.Instance);
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
    public GameRepo(IMongoDatabase database, IClock clock, ILogger logger)
    {
        var filter = new BsonDocument("name", CollectionName);
        var collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
        if (!collections.Any())
            database.CreateCollection(CollectionName);
        Collection = database.GetCollection<Game>(CollectionName);
        _clock = clock;
        this._logger = logger;
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

    public async Task<Game> CreateGame(string name, string creatorid, string orgid)
    {
        Game game = new Game(
            name: name,
            gameid: string.Empty,
            creatorid: creatorid,
            orgid: orgid,
            createdat: _clock.GetCurrentInstant(),
            isActive: true,
            defaultrole: Player.gameRole.Human,
            players: new HashSet<Player>()
            );
        await Collection.InsertOneAsync(game);
        GameUpdatedEventArgs gameCreatedEventArgs = new GameUpdatedEventArgs(game);
        _logger.LogTrace($"New game created in org {orgid} by user {creatorid}");
        OnGameCreated(gameCreatedEventArgs);
        return game;
    }

    public async Task<Game?> FindGameById(string id) =>
        id == string.Empty ? null : await Collection.Find<Game>(g => g.Id == id).FirstOrDefaultAsync();

    public async Task<Game> GetGameById(string id)
    {
        Game? game = await FindGameById(id);
        if (game == null)
            throw new ArgumentException($"Game with id \"{id}\" not found!");
        return (Game)game;
    }
    public async Task<Game?> FindGameByName(string name) =>
        await Collection.Find<Game>(g => g.Name == name).FirstOrDefaultAsync();

    public async Task<Game> GetGameByName(string name)
    {
        Game? game = await FindGameByName(name);
        if (game == null)
            throw new ArgumentException($"Game with name \"{name}\" not found!");
        return (Game)game;
    }

    public async Task<Player?> FindPlayerByUserId(string gameId, string userId)
    {
        Game game = await GetGameById(gameId);

        Player? player = game.Players.Where(p => p.UserId == userId).FirstOrDefault(defaultValue: null);

        return player;
    }

    public async Task<Player?> FindPlayerByGameId(string gameId, string userGameId)
    {
        Game game = await GetGameById(gameId);

        Player? player = game.Players.Where(p => p.GameId == userGameId).FirstOrDefault(defaultValue: null);

        return player;
    }

    public async Task<Game> AddPlayer(string gameId, string userId)
    {
        Game game = await GetGameById(gameId);
        if (FindPlayerByUserId(gameId, userId).Result != null)
            throw new ArgumentException($"User {userId} is already in Game {gameId}!");

        Player player = new Player(userId, await GeneratePlayerGameId(gameId), game.DefaultRole, 0, _clock.GetCurrentInstant());
        HashSet<Player> newPlayers = game.Players;
        newPlayers.Add(player);

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Id == gameId,
            Builders<Game>.Update.Set(g => g.Players, newPlayers),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );
        _logger.LogTrace($"User {userId} added to game {gameName}");
        OnPlayerJoined(new(game));
        return newGame;
    }

    public async Task<Game> SetActive(string gameId, bool active)
    {
        //TODO disallow if there is an active game in the org this game belongs to
        Game game = await GetGameById(gameId);

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Id == gameId,
            Builders<Game>.Update.Set(g => g.IsActive, active),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );
        _logger.LogTrace($"game {gameName} IsActive updated to {active}");
        OnGameUpdated(new(game));
        return newGame;
    }

    public async Task<Game> SetPlayerToRole(string gameId, string userId, Player.gameRole role)
    {
        Game game = await GetGameById(gameId);

        //TODO: this surely can be optimized
        Player? player = await FindPlayerByUserId(gameId, userId);
        if (player == null)
            throw new ArgumentException($"User {userId} not found in game {gameId}");

        var players = game.Players;
        players.Where(p => p.UserId == userId).First().Role = role;

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Id == gameId,
            Builders<Game>.Update.Set(g => g.Players, players),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );
        _logger.LogTrace($"User {userId} updated to role {role} in game {gameName}");
        OnPlayerRoleChanged(new(game));
        return newGame;
    }

    public async Task<Game> LogTag(string gameId, string taggerUserId, string tagRecieverGameId)
    {
        if (taggerUserId == tagRecieverGameId)
            throw new ArgumentException("players cannot tag themselves.");

        Game game = await GetGameById(gameId);

        if (!game.IsActive)
            throw new ArgumentException($"Game {gameId} is not active!");

        if (game.Players.Where(p => p.UserId == taggerUserId).Any() is false)
            throw new ArgumentException($"Player {taggerUserId} not found in Game {gameId}!");

        if (game.Players.Where(p => p.GameId == tagRecieverGameId).Any() is false)
            throw new ArgumentException($"Player {tagRecieverGameId} not found in Game {gameId}!");

        var Players = game.Players;
        Player.gameRole taggerRole = Players.Where(p => p.UserId == taggerUserId).First().Role;
        if (taggerRole != Player.gameRole.Zombie && taggerRole != Player.gameRole.Oz)
            throw new ArgumentException($"Tagger {taggerUserId} is not a zombie or OZ!");

        Player.gameRole tagRecieverRole = Players.Where(p => p.GameId == tagRecieverGameId).First().Role;
        if (tagRecieverRole != Player.gameRole.Human)
            throw new ArgumentException($"tagReciever {tagRecieverGameId} is not Human!");

        Players.Where(p => p.UserId == taggerUserId).First().Tags += 1;
        Players.Where(p => p.GameId == tagRecieverGameId).First().Role = Player.gameRole.Zombie;

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Id == gameId,
            Builders<Game>.Update.Set(g => g.Players, Players),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );

        _logger.LogTrace($"User {taggerUserId} tagged user {tagRecieverGameId} in game {gameName}");
        OnTag(new(game));
        return newGame;
    }

    private async Task<String> GeneratePlayerGameId(string gameId)
    {
        Game game = await GetGameById(gameId);

        int id;
        do
        {
            id = Random.Shared.Next(1000, 9999);
        } while (game.Players.Where(p => p.GameId == id.ToString()).Any());
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