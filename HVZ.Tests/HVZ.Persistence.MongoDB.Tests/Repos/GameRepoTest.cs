using HVZ.Persistence.Models;
using HVZ.Persistence.MongoDB.Repos;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using NodaTime;
namespace HVZ.Persistence.MongoDB.Tests;

[Parallelizable(ParallelScope.All)]
public class GameRepoTest : MongoTestBase
{
    public GameRepo CreateGameRepo() =>
            new GameRepo(CreateTemporaryDatabase(), Mock.Of<IClock>(), Mock.Of<ILogger>());
    private const string defaultTimeString = "1970-01-01T00:00:00Z";

    [Test]
    public async Task create_then_read_are_equal()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = Random.Shared.Next().ToString();
        string userid = "0";
        string orgid = "123";
        Game createdGame = await gameRepo.CreateGame(gameName, userid, orgid);
        Game foundGame = await gameRepo.Collection.Find(g => g.CreatorId == userid).FirstAsync();

        Assert.That(createdGame.Id, Is.EqualTo(foundGame.Id));
        Assert.That(foundGame.Id, Is.Not.EqualTo(String.Empty));
        Assert.That(createdGame.Id, Is.Not.EqualTo(String.Empty));
    }

    [Test]
    public async Task test_findgamebyid()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "1";
        string orgid = "123";
        Game createdGame = await gameRepo.CreateGame(gameName, userid, orgid);
        Game? foundGame = await gameRepo.FindGameById(createdGame.Id);
        Game? notFoundGame = await gameRepo.FindGameById(string.Empty);

        Assert.That(foundGame, Is.EqualTo(createdGame));
        Assert.That(notFoundGame, Is.Null);
    }

    [Test]
    public async Task test_getgamebyid()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "1";
        string orgid = "123";
        Game createdGame = await gameRepo.CreateGame(gameName, userid, orgid);
        Game foundGame = await gameRepo.GetGameById(createdGame.Id);

        Assert.That(foundGame, Is.EqualTo(createdGame));
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.GetGameById(""));
    }

    [Test]
    public async Task test_findgamebyname()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "1";
        string orgid = "123";
        Game createdGame = await gameRepo.CreateGame(gameName, userid, orgid);
        Game? foundGame = await gameRepo.FindGameByName(gameName);
        Game? notFoundGame = await gameRepo.FindGameByName(string.Empty);

        Assert.That(foundGame, Is.Not.Null);
        Assert.That(foundGame, Is.EqualTo(createdGame));
        Assert.That(notFoundGame, Is.Null);
    }

    [Test]
    public async Task test_getgamebyname()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "1";
        string orgid = "123";
        Game createdGame = await gameRepo.CreateGame(gameName, userid, orgid);
        Game foundGame = await gameRepo.GetGameByName(gameName);

        Assert.That(foundGame, Is.EqualTo(createdGame));
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.GetGameByName("none"));
    }

    [Test]
    public async Task test_findplayerbyuserid()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Player? p = await gameRepo.FindPlayerByUserId(game.Id, userid);
        Assert.That(p, Is.Null);
        await gameRepo.AddPlayer(game.Id, userid);
        p = await gameRepo.FindPlayerByUserId(game.Id, userid);
        Assert.That(p, Is.Not.Null);

    }

    [Test]
    public async Task test_getplayerbyuserid()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.GetPlayerByUserId(game.Id, userid));
        await gameRepo.AddPlayer(game.Id, userid);
        Player p = await gameRepo.GetPlayerByUserId(game.Id, userid);
        Assert.That(p.UserId, Is.EqualTo(userid));
    }

    [Test]
    public async Task test_findplayerbygameid()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Player? foundPlayer = await gameRepo.FindPlayerByGameId(game.Id, userid);
        Assert.That(foundPlayer, Is.Null);
        game = await gameRepo.AddPlayer(game.Id, userid);
        Player createdPlayer = game.Players.Where(p => p.UserId == userid).First();
        foundPlayer = await gameRepo.FindPlayerByGameId(game.Id, createdPlayer.GameId);
        Assert.That(foundPlayer, Is.Not.Null);

    }

    [Test]
    public async Task test_getplayerbygameid()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.GetPlayerByGameId(game.Id, userid));
        game = await gameRepo.AddPlayer(game.Id, userid);
        Player player = game.Players.Where(p => p.UserId == userid).First();
        Player p = await gameRepo.GetPlayerByGameId(game.Id, player.GameId);
        Assert.That(p, Is.EqualTo(player));
    }

    [Test]
    public async Task test_startgame()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Assert.That(game.Status == Game.GameStatus.New);
        Assert.That(game.StartedAt, Is.Null);

        game = await gameRepo.StartGame(game.Id, userid);
        Assert.That(game.Status == Game.GameStatus.Active);
        Assert.That(game.StartedAt, Is.Not.Null);
        Assert.That(game.StartedAt.ToString(), Is.EqualTo(defaultTimeString));
    }

    [Test]
    public async Task test_startgame_error_gamestarted()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.StartGame(game.Id, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.StartGame(game.Id, userid), $"Cannot start Game {game.Id} because it has already been started");
    }

    [Test]
    public async Task test_setpaused()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.StartGame(game.Id, userid);
        game = await gameRepo.SetGamePaused(game.Id, true, userid);
        Assert.That(game.Status, Is.EqualTo(Game.GameStatus.Paused));
        game = await gameRepo.SetGamePaused(game.Id, false, userid);
        Assert.That(game.Status, Is.EqualTo(Game.GameStatus.Active));
    }

    [Test]
    public async Task test_setpaused_error_gamenotstarted()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.SetGamePaused(game.Id, true, userid), $"Cannot set paused for Game {game.Id} because it has not been started yet");
    }

    [Test]
    public async Task test_setpaused_error_gameended()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);
        game = await gameRepo.EndGame(game.Id, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.SetGamePaused(game.Id, true, userid), $"Cannot set paused for Game {game.Id} because it has ended");
    }

    [Test]
    public async Task test_setpaused_error_duplicatestatus()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.SetGamePaused(game.Id, false, userid), $"Cannot set Game {game.Id} to {Game.GameStatus.Active} because it is already {Game.GameStatus.Active}");

        await gameRepo.SetGamePaused(game.Id, true, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.SetGamePaused(game.Id, true, userid), $"Cannot set Game {game.Id} to {Game.GameStatus.Paused} because it is already {Game.GameStatus.Paused}");
    }

    [Test]
    public async Task test_endgame()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);
        Assert.That(game.EndedAt, Is.Null);

        game = await gameRepo.EndGame(game.Id, userid);

        Assert.That(game.Status, Is.EqualTo(Game.GameStatus.Ended));
        Assert.That(game.EndedAt, Is.Not.Null);
        Assert.That(game.EndedAt.ToString(), Is.EqualTo(defaultTimeString));
    }

    [Test]
    public async Task test_endgame_error_gamenotstarted()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.EndGame(game.Id, userid), $"Cannot end Game {game.Id} because it has not started");
    }

    [Test]
    public async Task test_endgame_error_alreadyended()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);
        await gameRepo.EndGame(game.Id, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.EndGame(game.Id, userid), $"Cannot end Game {game.Id} because it has already ended");
    }

    [Test]
    public async Task test_addplayer_status_new()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);

        game = await gameRepo.GetGameById(game.Id);

        Assert.That(game.Players.Count, Is.EqualTo(1));
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.AddPlayer(game.Id, userid));

    }

    [Test]
    public async Task test_addplayer_status_active()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);
        await gameRepo.AddPlayer(game.Id, userid);

        game = await gameRepo.GetGameById(game.Id);

        Assert.That(game.Players.Count, Is.EqualTo(1));
    }

    public async Task test_addplayer_status_paused()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);
        await gameRepo.SetGamePaused(game.Id, true, userid);
        await gameRepo.AddPlayer(game.Id, userid);

        game = await gameRepo.GetGameById(game.Id);

        Assert.That(game.Players.Count, Is.EqualTo(1));
    }

    public async Task test_addplayer_status_ended()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);
        await gameRepo.EndGame(game.Id, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.AddPlayer(game.Id, userid),
            $"Cannot register for Game {game.Id} because registration has ended");
    }

    [Test]
    public async Task test_setplayerrole()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";
        Player.gameRole role = Player.gameRole.Oz;

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);

        game = await gameRepo.SetPlayerToRole(game.Id, userid, role, string.Empty);
        Assert.That(game.Players.First().Role, Is.EqualTo(role));
    }

    [Test]
    public async Task test_logtag()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid1 = "1";
        string userid2 = "2";
        string orgid = "123";
        string unregisteredUserId = "";

        Game game = await gameRepo.CreateGame(gameName, userid1, orgid);
        await gameRepo.AddPlayer(game.Id, userid1);
        game = await gameRepo.AddPlayer(game.Id, userid2);
        String user2gameid = game.Players.Where(p => p.UserId == userid2).First().GameId;

        //await gameRepo.SetGameStatus(game.Id, false, string.Empty);
        //tag while game is inactive
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie, string.Empty);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human, string.Empty);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));
        await gameRepo.StartGame(game.Id, userid1);
        await gameRepo.SetGamePaused(game.Id, true, userid1);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));

        await gameRepo.SetGamePaused(game.Id, false, userid1);
        //unregistered tags player
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, unregisteredUserId, userid1));
        //player tags unregistered
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, unregisteredUserId));
        //player tags self
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, userid1));
        //zombie tags zombie
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie, string.Empty);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Zombie, string.Empty);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));
        //zombie tags oz
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie, string.Empty);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Oz, string.Empty);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));
        //human tags human
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Human, string.Empty);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human, string.Empty);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));
        //zombie tags human
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie, string.Empty);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human, string.Empty);
        game = await gameRepo.LogTag(game.Id, userid1, user2gameid);
        Assert.That(game.Players.Where(p => p.UserId == userid2).First().Role, Is.EqualTo(Player.gameRole.Zombie));
        //oz tags human
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Oz, string.Empty);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human, string.Empty);
        game = await gameRepo.LogTag(game.Id, userid1, user2gameid);
        Assert.That(game.Players.Where(p => p.UserId == userid2).First().Role, Is.EqualTo(Player.gameRole.Zombie));
    }
    [Test]
    public async Task test_logtag_updates_tag_count()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid1 = "1";
        string userid2 = "2";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid1, orgid);
        await gameRepo.AddPlayer(game.Id, userid1);
        await gameRepo.AddPlayer(game.Id, userid2);
        game = await gameRepo.StartGame(game.Id, userid1);
        String gameid2 = game.Players.Where(p => p.UserId == userid2).First().GameId;

        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie, string.Empty);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human, string.Empty);
        game = await gameRepo.LogTag(game.Id, userid1, gameid2);

        int p1tags = game.Players.Where(p => p.UserId == userid1).First().Tags;
        int p2tags = game.Players.Where(p => p.UserId == userid2).First().Tags;
        Assert.That(p1tags, Is.EqualTo(1));
        Assert.That(p2tags, Is.EqualTo(0));
    }

    [Test]
    public async Task Test_getgameswithuser()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string otheruserid = "1";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);

        Assert.That(await gameRepo.GetGamesWithUser(userid), Is.Not.Empty);
        Assert.That(await gameRepo.GetGamesWithUser(otheruserid), Is.Empty);
    }

    [Test]
    public async Task Test_getactivegameswithuser()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);

        // Not in the game
        Assert.That(await gameRepo.GetCurrentGamesWithUser(userid), Is.Empty);

        // In game, status is new, game in list
        await gameRepo.AddPlayer(game.Id, userid);
        Assert.That(await gameRepo.GetCurrentGamesWithUser(userid), Is.Not.Empty);

        // Game status is active, game in list
        await gameRepo.StartGame(game.Id, userid);
        Assert.That(await gameRepo.GetCurrentGamesWithUser(userid), Is.Not.Empty);

        // Game status is paused, game in list
        await gameRepo.SetGamePaused(game.Id, true, userid);
        Assert.That(await gameRepo.GetCurrentGamesWithUser(userid), Is.Not.Empty);

        // Game status is ended, no games in list
        await gameRepo.EndGame(game.Id, userid);
        Assert.That(await gameRepo.GetCurrentGamesWithUser(userid), Is.Empty);
    }

    [Test]
    public async Task test_gamecreated_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.GameCreated += delegate (object? sender, GameUpdatedEventArgs args)
        {
            eventGame = args.game;
        };

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_playerjoined_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        Player? eventPlayer = null;
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.PlayerJoinedGame += delegate (object? sender, PlayerUpdatedEventArgs args)
        {
            eventGame = args.game;
            eventPlayer = args.player;
        };

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.AddPlayer(game.Id, userid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(eventPlayer, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_playerrolechanged_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        Player? eventPlayer = null;
        string? eventInstigator = null;
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.PlayerRoleChanged += delegate (object? sender, PlayerRoleChangedEventArgs args)
        {
            eventGame = args.game;
            eventPlayer = args.player;
            eventInstigator = args.instigatorId;
        };

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);
        game = await gameRepo.SetPlayerToRole(game.Id, userid, Player.gameRole.Oz, string.Empty);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(eventPlayer, Is.Not.Null);
        Assert.That(eventInstigator, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_taglogged_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        Player? eventTagger = null;
        Player? eventTagReciever = null;
        string gameName = "test";
        string userid1 = "1";
        string userid2 = "2";
        string orgid = "123";

        gameRepo.TagLogged += delegate (object? sender, TagEventArgs args)
        {
            eventGame = args.game;
            eventTagger = args.Tagger;
            eventTagReciever = args.TagReciever;
        };

        Game game = await gameRepo.CreateGame(gameName, userid1, orgid);
        await gameRepo.AddPlayer(game.Id, userid1);
        await gameRepo.AddPlayer(game.Id, userid2);
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie, string.Empty);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human, string.Empty);
        //game = await gameRepo.SetGameStatus(game.Id, true, string.Empty);
        game = await gameRepo.StartGame(game.Id, string.Empty);

        String gameid2 = game.Players.Where(p => p.UserId == userid2).First().GameId;

        game = await gameRepo.LogTag(game.Id, userid1, gameid2);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(eventTagger, Is.Not.Null);
        Assert.That(eventTagReciever, Is.Not.Null);
        Assert.That(eventTagReciever!.Role, Is.EqualTo(Player.gameRole.Zombie));
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_gamestarted_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        string? eventUpdatorId = null;
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.GameActiveStatusChanged += delegate (object? sender, GameStatusChangedEvent args)
        {
            eventGame = args.game;
            eventUpdatorId = args.updatorId;
        };

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.StartGame(game.Id, userid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(eventUpdatorId, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_gamepaused_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        string? eventUpdatorId = null;
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.GameActiveStatusChanged += delegate (object? sender, GameStatusChangedEvent args)
        {
            if (args.Status == Game.GameStatus.Paused)
            {
                eventGame = args.game;
                eventUpdatorId = args.updatorId;
            }
        };

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.StartGame(game.Id, userid);

        // Sanity check
        Assert.That(eventGame, Is.Null);
        Assert.That(eventUpdatorId, Is.Null);

        game = await gameRepo.SetGamePaused(game.Id, true, userid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(eventUpdatorId, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_endgame_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        string? eventUpdatorId = null;
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.GameActiveStatusChanged += delegate (object? sender, GameStatusChangedEvent args)
        {
            if (args.Status == Game.GameStatus.Ended)
            {
                eventGame = args.game;
                eventUpdatorId = args.updatorId;
            }
        };

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.StartGame(game.Id, userid);

        // Sanity check
        Assert.That(eventGame, Is.Null);
        Assert.That(eventUpdatorId, Is.Null);

        game = await gameRepo.EndGame(game.Id, userid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(eventUpdatorId, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_gamecreatedeventlog()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.GetGameById(game.Id);
        string logMessage = $"{defaultTimeString} Game {gameName} created by {userid}";

        Assert.That(game.EventLog.First().ToString(), Is.EqualTo(logMessage));
    }

    [Test]
    public async Task test_playerjoinedeventlog()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);
        game = await gameRepo.GetGameById(game.Id);
        string logMessage = $"{defaultTimeString} User {userid} joined the game";

        Assert.That(game.EventLog.Last().ToString(), Is.EqualTo(logMessage));
    }

    [Test]
    public async Task test_tageventlog()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string taggerid = "1";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Player user = (await gameRepo.AddPlayer(game.Id, userid)).Players.First();
        await gameRepo.AddPlayer(game.Id, taggerid);
        await gameRepo.StartGame(game.Id, userid);
        await gameRepo.SetPlayerToRole(game.Id, taggerid, Player.gameRole.Zombie, userid);
        await gameRepo.LogTag(game.Id, taggerid, user.GameId);

        game = await gameRepo.GetGameById(game.Id);
        string logMessage = $"{defaultTimeString} User {taggerid} tagged user {userid}";

        Assert.That(game.EventLog.Last().ToString(), Is.EqualTo(logMessage));
    }

    [Test]
    public async Task test_playerrolechangedbymodeventlog()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string otheruserid = "1";
        string orgid = "123";
        Player.gameRole role = Player.gameRole.Zombie;

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);
        await gameRepo.AddPlayer(game.Id, otheruserid);
        await gameRepo.SetPlayerToRole(game.Id, otheruserid, role, userid);

        game = await gameRepo.GetGameById(game.Id);
        string logMessage = $"{defaultTimeString} User {otheruserid} was set to {role.ToString()} by {userid}";

        Assert.That(game.EventLog.Last().ToString(), Is.EqualTo(logMessage));
    }

    [Test]
    public async Task test_startgameeventlog()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);
        await gameRepo.StartGame(game.Id, userid);

        game = await gameRepo.GetGameById(game.Id);
        string logMessage = $"{defaultTimeString} Game set to {Game.GameStatus.Active} by {userid}";

        Assert.That(game.EventLog.Last().ToString(), Is.EqualTo(logMessage));
    }

    [Test]
    public async Task test_pausegameeventlog()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);
        await gameRepo.StartGame(game.Id, userid);
        await gameRepo.SetGamePaused(game.Id, true, userid);

        game = await gameRepo.GetGameById(game.Id);
        string logMessage = $"{defaultTimeString} Game set to {Game.GameStatus.Paused} by {userid}";

        Assert.That(game.EventLog.Last().ToString(), Is.EqualTo(logMessage));

    }

    [Test]
    public async Task test_endgameeventlog()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);
        await gameRepo.StartGame(game.Id, userid);
        await gameRepo.EndGame(game.Id, userid);

        game = await gameRepo.GetGameById(game.Id);
        string logMessage = $"{defaultTimeString} Game set to {Game.GameStatus.Ended} by {userid}";

        Assert.That(game.EventLog.Last().ToString(), Is.EqualTo(logMessage));
    }
}