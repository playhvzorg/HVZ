namespace HVZ.Persistence.MongoDB.Repos;

using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Bson.Serialization.IdGenerators;
using global::MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Models;
using NodaTime;
using Serializers;

public class GameRepo : IGameRepo {
    private const string CollectionName = "Games";
    private readonly IClock clock;
    public readonly IMongoCollection<Game> Collection;
    private readonly ILogger logger;

    static GameRepo()
    {
        BsonClassMap.RegisterClassMap<Game>(cm => {
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

        BsonClassMap.RegisterClassMap<Player>(cm => {
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
        IAsyncCursor<BsonDocument>? collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
        if (!collections.Any())
            database.CreateCollection(CollectionName);
        Collection = database.GetCollection<Game>(CollectionName);
        this.clock = clock;
        this.logger = logger;
        InitIndexes();
    }

    public event EventHandler<GameUpdatedEventArgs>? GameCreated;
    public event EventHandler<PlayerUpdatedEventArgs>? PlayerJoinedGame;
    public event EventHandler<PlayerRoleChangedEventArgs>? PlayerRoleChanged;
    public event EventHandler<TagEventArgs>? TagLogged;
    public event EventHandler<GameActiveStatusChangedEventArgs>? GameActiveStatusChanged;

    public async Task<Game> CreateGame(string name, string creatorId, string orgId)
    {
        var game = new Game(
            name,
            string.Empty,
            creatorId,
            orgId,
            clock.GetCurrentInstant(),
            true,
            Player.GameRole.Human,
            new HashSet<Player>(),
            new List<GameEventLog>()
        );
        await Collection.InsertOneAsync(game);
        var gameCreatedEventArgs = new GameUpdatedEventArgs(game, creatorId);
        logger.LogTrace($"New game created in org {orgId} by user {creatorId}");
        await OnGameCreated(gameCreatedEventArgs);
        return game;
    }

    public async Task<Game?> FindGameById(string id)
    {
        return id == string.Empty ? null : await Collection.Find(g => g.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Game> GetGameById(string id)
    {
        Game? game = await FindGameById(id);
        if (game == null)
            throw new ArgumentException($"Game with id \"{id}\" not found!");
        return game;
    }

    public async Task<Game?> FindGameByName(string name)
    {
        return await Collection.Find(g => g.Name == name).FirstOrDefaultAsync();
    }

    public async Task<Game> GetGameByName(string name)
    {
        Game? game = await FindGameByName(name);
        if (game == null)
            throw new ArgumentException($"Game with name \"{name}\" not found!");
        return game;
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

        var player = new Player(userId, await GeneratePlayerGameId(gameId), game.DefaultRole, 0, clock.GetCurrentInstant());
        HashSet<Player> newPlayers = game.Players;
        newPlayers.Add(player);

        var newGame = await Collection.FindOneAndUpdateAsync<Game>(filter: g => g.Id == gameId,
            Builders<Game>.Update.Set(field: g => g.Players, newPlayers),
            new FindOneAndUpdateOptions<Game, Game>
                { ReturnDocument = ReturnDocument.After }
        );
        logger.LogTrace($"User {userId} added to game {game}");
        await OnPlayerJoined(new PlayerUpdatedEventArgs(game, player));
        return newGame;
    }

    public async Task<Game> SetActive(string gameId, bool active, string updatorId)
    {
        //TODO disallow if there is an active game in the org this game belongs to
        Game game = await GetGameById(gameId);

        var newGame = await Collection.FindOneAndUpdateAsync<Game>(filter: g => g.Id == gameId,
            Builders<Game>.Update.Set(field: g => g.IsActive, active),
            new FindOneAndUpdateOptions<Game, Game>
                { ReturnDocument = ReturnDocument.After }
        );
        logger.LogTrace($"game {game} IsActive updated to {active}");
        await OnGameActiveStatusChanged(new GameActiveStatusChangedEventArgs(game, updatorId, active));
        return newGame;
    }

    public async Task<Game> SetPlayerToRole(string gameId, string userId, Player.GameRole role, string instigatorId)
    {
        Game game = await GetGameById(gameId);

        //TODO: this surely can be optimized
        Player? player = await FindPlayerByUserId(gameId, userId);
        if (player == null)
            throw new ArgumentException($"User {userId} not found in game {gameId}");

        HashSet<Player> players = game.Players;
        players.First(p => p.UserId == userId).Role = role;
        player.Role = role;

        var newGame = await Collection.FindOneAndUpdateAsync<Game>(filter: g => g.Id == gameId,
            Builders<Game>.Update.Set(field: g => g.Players, players),
            new FindOneAndUpdateOptions<Game, Game>
                { ReturnDocument = ReturnDocument.After }
        );
        logger.LogTrace($"User {userId} updated to role {role} in game {game} by user {instigatorId}");
        await OnPlayerRoleChanged(new PlayerRoleChangedEventArgs(game, player, instigatorId, role));
        return newGame;
    }

    public async Task<Game> LogTag(string gameId, string taggerUserId, string tagReceiverGameId)
    {
        if (taggerUserId == tagReceiverGameId)
            throw new ArgumentException("players cannot tag themselves.");

        Game game = await GetGameById(gameId);

        if (!game.IsActive)
            throw new ArgumentException($"Game {gameId} is not active!");

        if (game.Players.Any(p => p.UserId == taggerUserId) is false)
            throw new ArgumentException($"Player {taggerUserId} not found in Game {gameId}!");

        if (game.Players.Any(p => p.GameId == tagReceiverGameId) is false)
            throw new ArgumentException($"Player {tagReceiverGameId} not found in Game {gameId}!");

        HashSet<Player> players = game.Players;
        Player.GameRole taggerRole = players.First(p => p.UserId == taggerUserId).Role;
        if (taggerRole != Player.GameRole.Zombie && taggerRole != Player.GameRole.Oz)
            throw new ArgumentException($"Tagger {taggerUserId} is not a zombie or OZ!");

        Player.GameRole tagReceiverRole = players.First(p => p.GameId == tagReceiverGameId).Role;
        if (tagReceiverRole != Player.GameRole.Human)
            throw new ArgumentException($"tagReciever {tagReceiverGameId} is not Human!");

        players.First(p => p.UserId == taggerUserId).Tags += 1;
        players.First(p => p.GameId == tagReceiverGameId).Role = Player.GameRole.Zombie;

        var newGame = await Collection.FindOneAndUpdateAsync<Game>(filter: g => g.Id == gameId,
            Builders<Game>.Update.Set(field: g => g.Players, players),
            new FindOneAndUpdateOptions<Game, Game>
                { ReturnDocument = ReturnDocument.After }
        );

        logger.LogTrace($"User {taggerUserId} tagged user {tagReceiverGameId} in game {game}");
        Player tagger = players.First(p => p.UserId == taggerUserId);
        Player tagReceiver = players.First(p => p.GameId == tagReceiverGameId);
        await OnTag(new TagEventArgs(game, tagger, tagReceiver));
        return newGame;
    }

    public async Task<List<Game>> GetGamesWithUser(string userId, int? limit = null)
    {
        return await Collection.Find(g => g.Players.Any(p => p.UserId == userId)).ToListAsync();
    }

    public async Task<List<Game>> GetActiveGamesWithUser(string userId, int? limit = null)
    {
        return (await GetGamesWithUser(userId, limit)).Where(g => g.IsActive).ToList();
    }

    public async Task<List<GameEventLog>> GetGameEventLog(string id)
    {
        return (await Collection.FindAsync(g => g.Id == id)).FirstOrDefault().EventLog;
    }

    public void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<Game>(Builders<Game>.IndexKeys.Ascending(g => g.CreatedAt)),
            new CreateIndexModel<Game>(Builders<Game>.IndexKeys.Ascending(g => g.Name)),
            new CreateIndexModel<Game>(Builders<Game>.IndexKeys.Ascending(g => g.Id))
        });
    }

    private async Task<string> GeneratePlayerGameId(string gameId)
    {
        Game game = await GetGameById(gameId);

        int id;
        do
        {
            id = Random.Shared.Next(1000, 9999);
        } while (game.Players.Any(p => p.GameId == id.ToString()));
        return id.ToString();
    }

    private async Task LogGameEvent(string gameId, GameEventLog log)
    {
        await Collection.FindOneAndUpdateAsync(filter: g => g.Id == gameId, Builders<Game>.Update.AddToSet(field: g => g.EventLog, log));
    }

    protected virtual async Task OnGameCreated(GameUpdatedEventArgs args)
    {
        EventHandler<GameUpdatedEventArgs>? handler = GameCreated;
        if (handler != null)
            handler(this, args);
        await LogGameEvent(args.Game.Id, new GameEventLog(GameEvent.GameCreated, clock.GetCurrentInstant(), args.Game.CreatorId, new Dictionary<string, object>
            { { "name", args.Game.Name } }));
    }

    protected virtual async Task OnPlayerJoined(PlayerUpdatedEventArgs args)
    {
        EventHandler<PlayerUpdatedEventArgs>? handler = PlayerJoinedGame;
        handler?.Invoke(this, args);
        await LogGameEvent(args.Game.Id, new GameEventLog(GameEvent.PlayerJoined, clock.GetCurrentInstant(), args.Player.UserId));
    }

    protected virtual async Task OnPlayerRoleChanged(PlayerRoleChangedEventArgs args)
    {
        EventHandler<PlayerRoleChangedEventArgs>? handler = PlayerRoleChanged;
        handler?.Invoke(this, args);
        await LogGameEvent(args.Game.Id, new GameEventLog(GameEvent.PlayerRoleChangedByMod, clock.GetCurrentInstant(), args.Player.UserId, new Dictionary<string, object> { { "modid", args.InstigatorId }, { "role", args.Role } }));
    }

    protected virtual async Task OnGameActiveStatusChanged(GameActiveStatusChangedEventArgs args)
    {
        EventHandler<GameActiveStatusChangedEventArgs>? handler = GameActiveStatusChanged;
        handler?.Invoke(this, args);
        await LogGameEvent(args.Game.Id, new GameEventLog(GameEvent.ActiveStatusChanged, clock.GetCurrentInstant(), args.UpdaterId, new Dictionary<string, object> { { "state", args.Active } }));
    }

    protected virtual async Task OnTag(TagEventArgs args)
    {
        EventHandler<TagEventArgs>? handler = TagLogged;
        handler?.Invoke(this, args);
        await LogGameEvent(args.Game.Id, new GameEventLog(GameEvent.Tag, clock.GetCurrentInstant(), args.Tagger.UserId, new Dictionary<string, object> { { "tagreciever", args.TagReceiver.UserId } }));
    }
}