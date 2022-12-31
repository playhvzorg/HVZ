using System.Threading.Tasks;
using NUnit.Framework;
using NodaTime;
using Moq;
using HVZ.Models;
using HVZ.Persistence.MongoDB.Repos;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
namespace HVZ.Persistence.MongoDB.Tests;

[Parallelizable(ParallelScope.All)]
public class GameRepoTest : MongoTestBase
{
    public GameRepo CreateGameRepo() =>
            new GameRepo(CreateTemporaryDatabase(), Mock.Of<IClock>(), Mock.Of<ILogger>());

    [Test]
    public async Task create_then_read_are_equal()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";
        Game createdGame = await gameRepo.CreateGame(gameName, userid, orgid);
        Game foundGame = await gameRepo.Collection.Find(g => g.CreatorId == userid).FirstAsync();

        Assert.That(createdGame.GameId, Is.EqualTo(foundGame.GameId));
        Assert.That(foundGame.GameId, Is.Not.EqualTo(String.Empty));
        Assert.That(createdGame.GameId, Is.Not.EqualTo(String.Empty));
    }

    [Test]
    public async Task test_findgamebyid()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "1";
        string orgid = "123";
        Game createdGame = await gameRepo.CreateGame(gameName, userid, orgid);
        Game? foundGame = await gameRepo.FindGameById(createdGame.GameId);
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
        Game foundGame = await gameRepo.GetGameById(createdGame.GameId);

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

        await gameRepo.CreateGame(gameName, userid, orgid);
        Player? p = await gameRepo.FindPlayerByUserId(gameName, userid);
        Assert.That(p, Is.Null);
        await gameRepo.AddPlayer(gameName, userid);
        p = await gameRepo.FindPlayerByUserId(gameName, userid);
        Assert.That(p, Is.Not.Null);

    }

    [Test]
    public async Task test_findplayerbygameid()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        await gameRepo.CreateGame(gameName, userid, orgid);
        Player? foundPlayer = await gameRepo.FindPlayerByGameId(gameName, userid);
        Assert.That(foundPlayer, Is.Null);
        Game game = await gameRepo.AddPlayer(gameName, userid);
        Player createdPlayer = game.Players.Where(p => p.UserId == userid).First();
        foundPlayer = await gameRepo.FindPlayerByGameId(gameName, createdPlayer.GameId);
        Assert.That(foundPlayer, Is.Not.Null);

    }

    [Test]
    public async Task test_setactive()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        await gameRepo.CreateGame(gameName, userid, orgid);
        Game game = await gameRepo.SetActive(gameName, true);
        Assert.That(game.IsActive, Is.True);
        game = await gameRepo.SetActive(gameName, false);
        Assert.That(game.IsActive, Is.False);
    }

    [Test]
    public async Task test_addplayer()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(gameName, userid);

        Game? game = await gameRepo.FindGameByName(gameName);
        if (game == null)
            throw new Exception();

        Assert.That(game.Players.Count, Is.EqualTo(1));
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.AddPlayer(gameName, userid));

    }

    [Test]
    public async Task test_setplayerrole()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";
        Player.gameRole role = Player.gameRole.Oz;

        await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(gameName, userid);

        Game game = await gameRepo.SetPlayerToRole(gameName, userid, role);
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

        await gameRepo.CreateGame(gameName, userid1, orgid);
        await gameRepo.AddPlayer(gameName, userid1);
        Game game = await gameRepo.AddPlayer(gameName, userid2);
        String gameid2 = game.Players.Where(p => p.UserId == userid2).First().GameId;

        //tag while game is inactive
        await gameRepo.SetPlayerToRole(gameName, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(gameName, userid2, Player.gameRole.Human);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(gameName, userid1, gameid2));

        await gameRepo.SetActive(gameName, true);
        //unregistered tags player
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(gameName, unregisteredUserId, userid1));
        //player tags unregistered
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(gameName, userid1, unregisteredUserId));
        //player tags self
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(gameName, userid1, userid1));
        //zombie tags zombie
        await gameRepo.SetPlayerToRole(gameName, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(gameName, userid2, Player.gameRole.Zombie);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(gameName, userid1, gameid2));
        //zombie tags oz
        await gameRepo.SetPlayerToRole(gameName, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(gameName, userid2, Player.gameRole.Oz);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(gameName, userid1, gameid2));
        //human tags human
        await gameRepo.SetPlayerToRole(gameName, userid1, Player.gameRole.Human);
        await gameRepo.SetPlayerToRole(gameName, userid2, Player.gameRole.Human);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(gameName, userid1, gameid2));
        //zombie tags human
        await gameRepo.SetPlayerToRole(gameName, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(gameName, userid2, Player.gameRole.Human);
        game = await gameRepo.LogTag(gameName, userid1, gameid2);
        Assert.That(game.Players.Where(p => p.UserId == userid2).First().Role, Is.EqualTo(Player.gameRole.Zombie));
        //oz tags human
        await gameRepo.SetPlayerToRole(gameName, userid1, Player.gameRole.Oz);
        await gameRepo.SetPlayerToRole(gameName, userid2, Player.gameRole.Human);
        game = await gameRepo.LogTag(gameName, userid1, gameid2);
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

        await gameRepo.CreateGame(gameName, userid1, orgid);
        await gameRepo.AddPlayer(gameName, userid1);
        await gameRepo.AddPlayer(gameName, userid2);
        Game game = await gameRepo.SetActive(gameName, true);
        String gameid2 = game.Players.Where(p => p.UserId == userid2).First().GameId;

        await gameRepo.SetPlayerToRole(gameName, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(gameName, userid2, Player.gameRole.Human);
        game = await gameRepo.LogTag(gameName, userid1, gameid2);

        int p1tags = game.Players.Where(p => p.UserId == userid1).First().Tags;
        int p2tags = game.Players.Where(p => p.UserId == userid2).First().Tags;
        Assert.That(p1tags, Is.EqualTo(1));
        Assert.That(p2tags, Is.EqualTo(0));
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
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.PlayerJoinedGame += delegate (object? sender, GameUpdatedEventArgs args)
        {
            eventGame = args.game;
        };

        await gameRepo.CreateGame(gameName, userid, orgid);
        Game game = await gameRepo.AddPlayer(gameName, userid);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_playerrolechanged_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.PlayerRoleChanged += delegate (object? sender, GameUpdatedEventArgs args)
        {
            eventGame = args.game;
        };

        await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(gameName, userid);
        Game game = await gameRepo.SetPlayerToRole(gameName, userid, Player.gameRole.Oz);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_taglogged_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        string gameName = "test";
        string userid1 = "1";
        string userid2 = "2";
        string orgid = "123";

        gameRepo.TagLogged += delegate (object? sender, GameUpdatedEventArgs args)
        {
            eventGame = args.game;
        };

        await gameRepo.CreateGame(gameName, userid1, orgid);
        await gameRepo.AddPlayer(gameName, userid1);
        await gameRepo.AddPlayer(gameName, userid2);
        await gameRepo.SetPlayerToRole(gameName, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(gameName, userid2, Player.gameRole.Human);
        Game game = await gameRepo.SetActive(gameName, true);

        String gameid2 = game.Players.Where(p => p.UserId == userid2).First().GameId;

        game = await gameRepo.LogTag(gameName, userid1, gameid2);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }

    [Test]
    public async Task test_gameupdated_event()
    {
        GameRepo gameRepo = CreateGameRepo();
        Game? eventGame = null;
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        gameRepo.GameUpdated += delegate (object? sender, GameUpdatedEventArgs args)
        {
            eventGame = args.game;
        };

        await gameRepo.CreateGame(gameName, userid, orgid);
        Game game = await gameRepo.SetActive(gameName, true);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }
}