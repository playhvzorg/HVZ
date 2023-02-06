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
    public event EventHandler<PlayerUpdatedEventArgs>? PlayerJoinedGame;
    public event EventHandler<PlayerRoleChangedEventArgs>? PlayerRoleChanged;
    public event EventHandler<TagEventArgs>? TagLogged;
    public event EventHandler<GameUpdatedEventArgs>? GameUpdated;
    public event EventHandler<GameActiveStatusChangedEventArgs>? GameActiveStatusChanged;

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
            cm.MapProperty(g => g.EventLog);
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
            players: new HashSet<Player>(),
            eventLog: new List<GameEventLog>()
            );
        await Collection.InsertOneAsync(game);
        GameUpdatedEventArgs gameCreatedEventArgs = new GameUpdatedEventArgs(game, creatorid);
        _logger.LogTrace($"New game created in org {orgid} by user {creatorid}");
        await OnGameCreated(gameCreatedEventArgs);
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
        _logger.LogTrace($"User {userId} added to game {game}");
        await OnPlayerJoined(new(game, player));
        return newGame;
    }

    public async Task<Game> SetActive(string gameId, bool active, string updatorId)
    {
        //TODO disallow if there is an active game in the org this game belongs to
        Game game = await GetGameById(gameId);

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Id == gameId,
            Builders<Game>.Update.Set(g => g.IsActive, active),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );
        _logger.LogTrace($"game {game} IsActive updated to {active}");
        await OnGameActiveStatusChanged(new(game, updatorId, active));
        return newGame;
    }

    public async Task<Game> SetPlayerToRole(string gameId, string userId, Player.gameRole role, string instigatorId)
    {
        Game game = await GetGameById(gameId);

        //TODO: this surely can be optimized
        Player? player = await FindPlayerByUserId(gameId, userId);
        if (player == null)
            throw new ArgumentException($"User {userId} not found in game {gameId}");

        var players = game.Players;
        players.Where(p => p.UserId == userId).First().Role = role;
        player.Role = role;

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Id == gameId,
            Builders<Game>.Update.Set(g => g.Players, players),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );
        _logger.LogTrace($"User {userId} updated to role {role} in game {game} by user {instigatorId}");
        await OnPlayerRoleChanged(new(game, player, instigatorId, role));
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

        _logger.LogTrace($"User {taggerUserId} tagged user {tagRecieverGameId} in game {game}");
        Player tagger = Players.Where(p => p.UserId == taggerUserId).First();
        Player tagReciever = Players.Where(p => p.GameId == tagRecieverGameId).First();
        await OnTag(new(game, tagger, tagReciever));
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

    public async Task<List<Game>> GetGamesWithUser(string userId, int? limit = null)
        => await Collection.Find<Game>(g => g.Players.Where(p => p.UserId == userId).Any()).ToListAsync();

    public async Task<List<Game>> GetActiveGamesWithUser(string userId, int? limit = null)
        => (await GetGamesWithUser(userId, limit)).Where(g => g.IsActive).ToList();

    public async Task<List<GameEventLog>> GetGameEventLog(string id)
        => (await Collection.FindAsync(g => g.Id == id)).FirstOrDefault().EventLog;

    private async Task LogGameEvent(string gameId, GameEventLog log)
    {
        await Collection.FindOneAndUpdateAsync(g => g.Id == gameId, Builders<Game>.Update.AddToSet(g => g.EventLog, log));
    }

    protected virtual async Task OnGameCreated(GameUpdatedEventArgs args)
    {
        EventHandler<GameUpdatedEventArgs>? handler = GameCreated;
        if (handler != null)
        {
            handler(this, args);
        }
        await LogGameEvent(args.game.Id, new(GameEvent.GameCreated, _clock.GetCurrentInstant(), args.game.CreatorId, new Dictionary<string, object>() { { "name", args.game.Name } }));
    }

    protected virtual async Task OnPlayerJoined(PlayerUpdatedEventArgs args)
    {
        EventHandler<PlayerUpdatedEventArgs>? handler = PlayerJoinedGame;
        if (handler != null)
        {
            handler(this, args);
        }
        await LogGameEvent(args.game.Id, new(GameEvent.PlayerJoined, _clock.GetCurrentInstant(), args.player.UserId));
    }
    protected virtual async Task OnPlayerRoleChanged(PlayerRoleChangedEventArgs args)
    {
        EventHandler<PlayerRoleChangedEventArgs>? handler = PlayerRoleChanged;
        if (handler != null)
        {
            handler(this, args);
        }
        await LogGameEvent(args.game.Id, new(GameEvent.PlayerRoleChangedByMod, _clock.GetCurrentInstant(), args.player.UserId, new Dictionary<string, object> { { "modid", args.instigatorId }, { "role", args.Role } }));
    }
    protected virtual async Task OnGameActiveStatusChanged(GameActiveStatusChangedEventArgs args)
    {
        EventHandler<GameActiveStatusChangedEventArgs>? handler = GameActiveStatusChanged;
        if (handler != null)
        {
            handler(this, args);
        }
        await LogGameEvent(args.game.Id, new(GameEvent.ActiveStatusChanged, _clock.GetCurrentInstant(), args.updatorId, new Dictionary<string, object> { { "state", args.Active } }));
    }
    protected virtual async Task OnTag(TagEventArgs args)
    {
        EventHandler<TagEventArgs>? handler = TagLogged;
        if (handler != null)
        {
            handler(this, args);
        }
        await LogGameEvent(args.game.Id, new(GameEvent.Tag, _clock.GetCurrentInstant(), args.Tagger.UserId, new Dictionary<string, object> { { "tagreciever", args.TagReciever.UserId } }));
    }
}