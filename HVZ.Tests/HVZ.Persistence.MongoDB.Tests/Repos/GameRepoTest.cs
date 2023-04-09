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
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.StartGame(game.Id, userid),
            $"Cannot start Game {game.Id} because it has already been started");
    }

    [Test]
    public async Task test_pausegame()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.StartGame(game.Id, userid);
        game = await gameRepo.PauseGame(game.Id, userid);
        Assert.That(game.Status, Is.EqualTo(Game.GameStatus.Paused));
        game = await gameRepo.ResumeGame(game.Id, userid);
        Assert.That(game.Status, Is.EqualTo(Game.GameStatus.Active));
    }

    [Test]
    public async Task test_pausegame_exception_gamenotstarted()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.PauseGame(game.Id, userid),
            $"Cannot set paused for Game {game.Id} because it has not been started yet");
    }

    [Test]
    public async Task test_pausegame_exception_gameended()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);
        game = await gameRepo.EndGame(game.Id, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.PauseGame(game.Id, userid),
            $"Cannot set paused for Game {game.Id} because it has ended");
    }

    [Test]
    public async Task test_pausegame_exception_alreadypaused()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);

        await gameRepo.PauseGame(game.Id, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.PauseGame(game.Id, userid),
            $"Cannot set Game {game.Id} to {Game.GameStatus.Paused} because it is already {Game.GameStatus.Paused}");
    }

    [Test]
    public async Task test_resumegame_exception_alreadyactive()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.StartGame(game.Id, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.ResumeGame(game.Id, userid),
            $"Cannot set Game {game.Id} to {Game.GameStatus.Active} because it is already {Game.GameStatus.Active}");
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
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.EndGame(game.Id, userid),
            $"Cannot end Game {game.Id} because it has not started");
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
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.EndGame(game.Id, userid),
            $"Cannot end Game {game.Id} because it has already ended");
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
        await gameRepo.PauseGame(game.Id, userid);
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
        await gameRepo.PauseGame(game.Id, userid1);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));

        await gameRepo.ResumeGame(game.Id, userid1);
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
        await gameRepo.PauseGame(game.Id, userid);
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

        gameRepo.GameCreated += delegate (object? sender, GameUpdatedEventArgs args) { eventGame = args.game; };

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

        game = await gameRepo.PauseGame(game.Id, userid);

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
        await gameRepo.PauseGame(game.Id, userid);

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

    [Test]
    public async Task test_joinozpool()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);

        Assert.That(game.OzPool.Count(), Is.EqualTo(0));
        game = await gameRepo.AddPlayerToOzPool(game.Id, userid);
        Assert.That(game.OzPool.Count(), Is.EqualTo(1));
        Assert.That(game.OzPool.ToList()[0], Is.EqualTo(userid));
    }

    [Test]
    public async Task test_joinozpool_error_playernotingame()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.AddPlayerToOzPool(game.Id, "12345"),
            $"Could not find player with UserId 12345 in Game {game.Id}");
    }

    [Test]
    public async Task test_joinozpool_error_alreadyinpool()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);

        await gameRepo.AddPlayerToOzPool(game.Id, userid);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.AddPlayerToOzPool(game.Id, userid),
            $"Player with UserId {userid} is already in OZ Pool for game {game.Id}");
    }

    [Test]
    public async Task test_joinozpool_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        string? eventPlayerId = null;
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.PlayerJoinedOzPool += delegate (object? sender, OzPoolUpdatedEventArgs args)
        {
            eventPlayerId = args.playerId;
            eventGame = args.game;
        };

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);

        await gameRepo.AddPlayer(game.Id, userid);
        game = await gameRepo.AddPlayerToOzPool(game.Id, userid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));

        Assert.That(eventPlayerId, Is.Not.Null);
        Assert.That(eventPlayerId, Is.EqualTo(userid));
    }

    [Test]
    public async Task test_leaveozpool()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);
        game = await gameRepo.AddPlayerToOzPool(game.Id, userid);
        game = await gameRepo.RemovePlayerFromOzPool(game.Id, userid);

        Assert.That(game.OzPool.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task test_leaveozpool_error_notingame()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);

        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.RemovePlayerFromOzPool(game.Id, "12345"),
            $"Could not find player with UserId 12345 in Game {game.Id}");
    }

    [Test]
    public async Task test_leaveozpool_error_notinpool()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);

        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.RemovePlayerFromOzPool(game.Id, userid),
            $"Player with UserId {userid} is not in the OZ pool for Game {game.Id}");
    }

    [Test]
    public async Task test_leaveozpool_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";
        Game? eventGame = null!;
        string? eventPlayerId = null!;

        gameRepo.PlayerLeftOzPool += delegate (object? sender, OzPoolUpdatedEventArgs args)
        {
            eventGame = args.game;
            eventPlayerId = args.playerId;
        };

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);

        await gameRepo.AddPlayerToOzPool(game.Id, userid);
        game = await gameRepo.RemovePlayerFromOzPool(game.Id, userid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));

        Assert.That(eventPlayerId, Is.Not.Null);
        Assert.That(eventPlayerId, Is.EqualTo(userid));
    }

    [TestCase(1, 1, 2)]
    [TestCase(2, 2, 1)]
    [TestCase(3, 3, 0)]
    public async Task test_randomozs(int count, int numSelected, int numRemaining)
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string creatorid = "0";
        string orgid = "123";

        string userid1 = "1";
        string userid2 = "2";
        string userid3 = "3";

        Game game = await gameRepo.CreateGame(gameName, creatorid, orgid);
        await gameRepo.AddPlayer(game.Id, userid1);
        await gameRepo.AddPlayer(game.Id, userid2);
        await gameRepo.AddPlayer(game.Id, userid3);

        await gameRepo.AddPlayerToOzPool(game.Id, userid1);
        await gameRepo.AddPlayerToOzPool(game.Id, userid2);
        await gameRepo.AddPlayerToOzPool(game.Id, userid3);

        game = await gameRepo.AssignRandomOzs(game.Id, count, creatorid);

        Assert.That(game.OzPool.Count(), Is.EqualTo(numRemaining));
        Assert.That(game.Ozs.Count(), Is.EqualTo(numSelected));
    }

    [Test]
    public async Task test_randomozs_count_too_large_throws_exception()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);

        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.AssignRandomOzs(game.Id, 2, userid));
    }

    [Test]
    public async Task test_randomozs_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string creatorid = "0";
        string orgid = "123";
        int randomOzCount = 2;
        List<string> playerIds = new List<string>();
        Game? eventGame = null;
        string[]? ozIds = null;

        gameRepo.RandomOzsSet += delegate (object? sender, RandomOzEventArgs args)
        {
            eventGame = args.game;
            ozIds = args.randomOzIds;
        };

        Game game = await gameRepo.CreateGame(gameName, creatorid, orgid);

        for (int i = 1; i < 5; i++)
        {
            game = await gameRepo.AddPlayer(game.Id, i.ToString());
        }

        foreach (Player player in game.Players)
        {
            await gameRepo.AddPlayerToOzPool(game.Id, player.UserId);
            playerIds.Add(player.UserId);
        }

        game = await gameRepo.AssignRandomOzs(game.Id, randomOzCount, creatorid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(eventGame, Is.EqualTo(game));

        Assert.That(ozIds, Is.Not.Null);
        Assert.That(ozIds.Length, Is.EqualTo(randomOzCount));

        foreach (string id in ozIds)
        {
            Assert.That(playerIds.Contains(id), Is.True);
        }
    }

    [Test]
    public async Task test_initializetagcount()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid1 = "123";
        string orgid2 = "456";
        int game2maxoztags = 15;

        Game game = await gameRepo.CreateGame(gameName, userid, orgid1);

        Assert.That(game.OzMaxTags, Is.EqualTo(3));

        Game game2 = await gameRepo.CreateGame(gameName, userid, orgid2, game2maxoztags);

        Assert.That(game2.OzMaxTags, Is.EqualTo(game2maxoztags));
    }

    [Test]
    public async Task test_setmaxtags()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";
        int newTagCount = 2;

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);

        game = await gameRepo.SetOzTagCount(game.Id, newTagCount, userid);

        Assert.That(game.OzMaxTags, Is.EqualTo(newTagCount));
    }

    [Test]
    public async Task test_setmaxtags_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";
        int newTagCount = 2;
        Game? eventGame = null;
        string? eventUpdator = null;

        gameRepo.GameSettingsChanged += delegate (object? sender, GameUpdatedEventArgs args)
        {
            eventGame = args.game;
            eventUpdator = args.updatorId;
        };

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.SetOzTagCount(game.Id, newTagCount, userid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(eventGame, Is.EqualTo(game));

        Assert.That(eventUpdator, Is.Not.Null);
        Assert.That(eventUpdator, Is.EqualTo(userid));
    }

    [Test]
    public async Task test_getmaxtags()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);

        int gameTagCount = await gameRepo.GetOzTagCount(game.Id);

        Assert.That(gameTagCount, Is.EqualTo(game.OzMaxTags));
    }
}