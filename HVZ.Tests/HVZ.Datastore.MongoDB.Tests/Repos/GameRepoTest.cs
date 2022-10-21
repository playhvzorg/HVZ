using System.Threading.Tasks;
using NUnit.Framework;
using NodaTime;
using Moq;
using HVZ.Models;
using HVZ.Datastore.MongoDB.Repos;
using MongoDB.Driver;
namespace HVZ.Datastore.MongoDB.Tests;

[Parallelizable(ParallelScope.All)]
public class GameRepoTest : MongoTestBase
{
    public GameRepo CreateGameRepo() =>
            new GameRepo(CreateTemporaryDatabase(), Mock.Of<IClock>());

    [Test]
    public async Task create_then_read_are_equal()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "0";
        Game createdGame = await gameRepo.CreateGame(gameName, userid);
        Game foundGame = await gameRepo.Collection.Find(g => g.UserId == userid).FirstAsync();

        Assert.That(createdGame.Id, Is.EqualTo(foundGame.Id));
        Assert.That(foundGame.Id, Is.Not.EqualTo(String.Empty));
        Assert.That(createdGame.Id, Is.Not.EqualTo(String.Empty));
    }

    [Test]
    public async Task test_find_by_id()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "1";
        Game createdGame = await gameRepo.CreateGame(gameName, userid);
        Game? foundGame = await gameRepo.FindById(createdGame.Id);
        Game? notFoundGame = await gameRepo.FindById(string.Empty);

        Assert.That(foundGame, Is.EqualTo(createdGame));
        Assert.That(notFoundGame, Is.Null);
    }

    [Test]
    public async Task test_find_by_name()
    {
        GameRepo gameRepo = CreateGameRepo();
        string gameName = "test";
        string userid = "1";
        Game createdGame = await gameRepo.CreateGame(gameName, userid);
        Game? foundGame = await gameRepo.FindByName(gameName);
        Game? notFoundGame = await gameRepo.FindByName(string.Empty);

        Assert.That(foundGame, Is.EqualTo(createdGame));
        Assert.That(notFoundGame, Is.Null);
    }
}