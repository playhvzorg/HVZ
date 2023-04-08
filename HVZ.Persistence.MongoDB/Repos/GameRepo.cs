using HVZ.Persistence.Models;
using HVZ.Persistence.MongoDB.Serializers;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;

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
    public event EventHandler<GameStatusChangedEvent>? GameActiveStatusChanged;

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
            cm.MapProperty(g => g.Status);
            cm.MapProperty(g => g.StartedAt)
                .SetSerializer(NullableInstantSerializer.Instance);
            cm.MapProperty(g => g.EndedAt)
                .SetSerializer(NullableInstantSerializer.Instance);
            cm.MapProperty(g => g.EventLog);
        });

        BsonClassMap.RegisterClassMap<Player>(cm =>
        {
            cm.MapProperty(p => p.UserId);
            cm.MapProperty(p => p.GameId);
            cm.MapProperty(p => p.Role);
            cm.MapProperty(p => p.Tags);
            cm.MapProperty(p => p.JoinedGameAt)
                .SetSerializer(InstantSerializer.Instance);
            cm.MapProperty(p => p.GameId);
        });

        BsonClassMap.RegisterClassMap<GameEventLog>(cm =>
        {
            cm.MapProperty(e => e.UserId);
            cm.MapProperty(e => e.GameEvent);
            cm.MapProperty(e => e.Timestamp)
                .SetSerializer(InstantSerializer.Instance);
            cm.MapProperty(e => e.AdditionalInfo);
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
            status: Game.GameStatus.New,
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

    public async Task<Player> GetPlayerByUserId(string gameId, string userId)
    {
        Player? player = await FindPlayerByUserId(gameId, userId);
        if (player is null)
            throw new ArgumentException($"Could not find player with UserId {userId} in Game {gameId}");
        return player;
    }

    public async Task<Player?> FindPlayerByGameId(string gameId, string userGameId)
    {
        Game game = await GetGameById(gameId);

        Player? player = game.Players.Where(p => p.GameId == userGameId).FirstOrDefault(defaultValue: null);

        return player;
    }

    public async Task<Player> GetPlayerByGameId(string gameId, string userGameId)
    {
        Player? player = await FindPlayerByGameId(gameId, userGameId);
        if (player is null)
            throw new ArgumentException($"Could not find player with GameId {userGameId} in Game {gameId}");
        return player;
    }

    public async Task<Game> AddPlayer(string gameId, string userId)
    {
        Game game = await GetGameById(gameId);
        if (!game.IsCurrent)
            throw new ArgumentException($"Cannot register for Game {gameId} because registration has ended");
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

    public async Task<Game> StartGame(string gameId, string instigatorId)
    {
        Game game = await GetGameById(gameId);

        if (game.Status != Game.GameStatus.New)
        {
            throw new ArgumentException($"Cannot start Game {gameId} because it has already been started");
        }

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Id == gameId,
            Builders<Game>.Update
                .Set(g => g.Status, Game.GameStatus.Active)
                .Set(g => g.StartedAt, _clock.GetCurrentInstant()),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );

        await OnGameActiveStatusChanged(new(newGame, instigatorId, Game.GameStatus.Active));
        return newGame;
    }

    public async Task<Game> PauseGame(string gameId, bool paused, string instigatorId)
    {
        Game game = await GetGameById(gameId);

        if (game.Status == Game.GameStatus.New)
        {
            throw new ArgumentException($"Cannot set paused for Game {gameId} because it has not been started yet");
        }

        if (game.Status == Game.GameStatus.Ended)
        {
            throw new ArgumentException($"Cannot set paused for Game {gameId} because it has ended");
        }

        Game.GameStatus status = paused ? Game.GameStatus.Paused : Game.GameStatus.Active;
        Console.WriteLine(status);

        if (game.Status == status)
        {
            throw new ArgumentException($"Cannot set Game {gameId} to {status} because it is already {status}");
        }

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Id == gameId,
            Builders<Game>.Update.Set(g => g.Status, status),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );

        await OnGameActiveStatusChanged(new(newGame, instigatorId, status));
        return newGame;
    }

    public async Task<Game> EndGame(string gameId, string instigatorId)
    {
        Game game = await GetGameById(gameId);

        if (game.Status == Game.GameStatus.New)
        {
            throw new ArgumentException($"Cannot end Game {gameId} because it has not started");
        }

        if (game.Status == Game.GameStatus.Ended)
        {
            throw new ArgumentException($"Cannot end Game {gameId} because it has already ended");
        }

        Game newGame = await Collection.FindOneAndUpdateAsync<Game>(g => g.Id == gameId,
            Builders<Game>.Update
                .Set(g => g.Status, Game.GameStatus.Ended)
                .Set(g => g.EndedAt, _clock.GetCurrentInstant()),
            new FindOneAndUpdateOptions<Game, Game>() { ReturnDocument = ReturnDocument.After }
        );

        await OnGameActiveStatusChanged(new(newGame, instigatorId, Game.GameStatus.Ended));
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

    public async Task<List<Game>> GetCurrentGamesWithUser(string userId, int? limit = null)
        => (await GetGamesWithUser(userId, limit)).Where(g => g.IsCurrent).ToList();

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
    protected virtual async Task OnGameActiveStatusChanged(GameStatusChangedEvent args)
    {
        EventHandler<GameStatusChangedEvent>? handler = GameActiveStatusChanged;
        if (handler != null)
        {
            handler(this, args);
        }
        await LogGameEvent(args.game.Id, new(GameEvent.ActiveStatusChanged, _clock.GetCurrentInstant(), args.updatorId, new Dictionary<string, object> { { "state", args.Status } }));
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