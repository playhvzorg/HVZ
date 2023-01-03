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
    public async Task test_setactive()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.SetActive(game.Id, true);
        Assert.That(game.IsActive, Is.True);
        game = await gameRepo.SetActive(game.Id, false);
        Assert.That(game.IsActive, Is.False);
    }

    [Test]
    public async Task test_addplayer()
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
    public async Task test_setplayerrole()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        string orgid = "123";
        Player.gameRole role = Player.gameRole.Oz;

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);

        game = await gameRepo.SetPlayerToRole(game.Id, userid, role);
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

        await gameRepo.SetActive(game.Id, false);
        //tag while game is inactive
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));

        await gameRepo.SetActive(game.Id, true);
        //unregistered tags player
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, unregisteredUserId, userid1));
        //player tags unregistered
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, unregisteredUserId));
        //player tags self
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, userid1));
        //zombie tags zombie
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Zombie);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));
        //zombie tags oz
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Oz);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));
        //human tags human
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Human);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human);
        Assert.ThrowsAsync<ArgumentException>(() => gameRepo.LogTag(game.Id, userid1, user2gameid));
        //zombie tags human
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human);
        game = await gameRepo.LogTag(game.Id, userid1, user2gameid);
        Assert.That(game.Players.Where(p => p.UserId == userid2).First().Role, Is.EqualTo(Player.gameRole.Zombie));
        //oz tags human
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Oz);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human);
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
        game = await gameRepo.SetActive(game.Id, true);
        String gameid2 = game.Players.Where(p => p.UserId == userid2).First().GameId;

        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human);
        game = await gameRepo.LogTag(game.Id, userid1, gameid2);

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

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.AddPlayer(game.Id, userid);

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

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        await gameRepo.AddPlayer(game.Id, userid);
        game = await gameRepo.SetPlayerToRole(game.Id, userid, Player.gameRole.Oz);

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

        Game game = await gameRepo.CreateGame(gameName, userid1, orgid);
        await gameRepo.AddPlayer(game.Id, userid1);
        await gameRepo.AddPlayer(game.Id, userid2);
        await gameRepo.SetPlayerToRole(game.Id, userid1, Player.gameRole.Zombie);
        await gameRepo.SetPlayerToRole(game.Id, userid2, Player.gameRole.Human);
        game = await gameRepo.SetActive(game.Id, true);

        String gameid2 = game.Players.Where(p => p.UserId == userid2).First().GameId;

        game = await gameRepo.LogTag(game.Id, userid1, gameid2);

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

        Game game = await gameRepo.CreateGame(gameName, userid, orgid);
        game = await gameRepo.SetActive(game.Id, true);

        Assert.That(eventGame, Is.Not.Null);
        Assert.That(game, Is.EqualTo(eventGame));
    }
}