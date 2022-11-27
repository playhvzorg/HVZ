using NodaTime;
using Moq;
using HVZ.Models;
using HVZ.Persistence.MongoDB.Repos;
using MongoDB.Driver;
namespace HVZ.Persistence.MongoDB.Tests;

public class OrgRepotest : MongoTestBase
{
    private OrgRepo orgRepo = null!;
    private Mock<IUserRepo> userRepoMock = null!;
    private Mock<IGameRepo> gameRepoMock = null!;
    //public OrgRepo CreateOrgRepo() =>
    //        new OrgRepo(CreateTemporaryDatabase(), Mock.Of<IClock>(), Mock.Of<IUserRepo>(), Mock.Of<IGameRepo>());

    [SetUp]
    public void SetUp()
    {
    userRepoMock = new Mock<IUserRepo>();
    gameRepoMock = new Mock<IGameRepo>();
    orgRepo = new OrgRepo(CreateTemporaryDatabase(), Mock.Of<IClock>(), userRepoMock.Object, gameRepoMock.Object);

    }

    [Test]
    public async Task create_then_read_are_equal()
    {
        string orgname = "test";
        string userid = "0";

        
        Organization createdOrg = await orgRepo.CreateOrg(orgname, userid);
        Organization foundOrg = await orgRepo.Collection.Find(o => o.OwnerId == userid).FirstAsync();

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg.Id, Is.Not.EqualTo(String.Empty));
        Assert.That(createdOrg.Id, Is.Not.EqualTo(String.Empty));
    }

    [Test]
    public async Task test_findorgbyid()
    {
        string orgname = "test";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, userid);
        Organization? foundOrg = await orgRepo.FindOrgById(createdOrg.Id);
        Organization? notFoundOrg = await orgRepo.FindOrgById(string.Empty);

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg, Is.Not.Null);
        Assert.That(notFoundOrg, Is.Null);
    }

    public async Task test_getorgbyid()
    {
        string orgname = "test";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, userid);
        Organization foundOrg = await orgRepo.GetOrgById(createdOrg.Id);
        
        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.GetOrgById("none"));
        
    }

    [Test]
    public async Task test_findorgbyname()
    {
        string orgname = "test";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, userid);
        Organization? foundOrg = await orgRepo.FindOrgByName(orgname);
        Organization? notFoundOrg = await orgRepo.FindOrgByName(string.Empty);

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg, Is.Not.Null);
        Assert.That(notFoundOrg, Is.Null);
    }

    [Test]
    public async Task test_setactivegameoforg()
    {
        string orgname = "test";
        string userid = "0";
        string gameid = "1";

        Organization org = await orgRepo.CreateOrg(orgname, userid);

        Assert.That(org.ActiveGameId, Is.Null);
        
        org = await orgRepo.SetActiveGameOfOrg(org.Id, gameid);

        Assert.That(org.ActiveGameId, Is.EqualTo(gameid));
    }

    [Test]
    public async Task test_getactivegameoforg()
    {
        string orgname = "test";
        string userid = "0";
        string gameid = "1";
        Organization org = await orgRepo.CreateOrg(orgname, userid);
        Game newGame = new("test", gameid, userid, org.Id, Instant.MinValue, true, Player.gameRole.Human, new HashSet<Player>());
        gameRepoMock.Setup(repo => repo.FindGameById("1")).ReturnsAsync(newGame);
        await orgRepo.SetActiveGameOfOrg(org.Id, gameid);

        Game? foundGame = await orgRepo.FindActiveGameOfOrg(org.Id);

        Assert.That(foundGame, Is.Not.Null);
        Assert.That(foundGame, Is.EqualTo(newGame));
    }

    [Test]
    public async Task test_getorgadmins()
    {
        string orgname = "test";
        string userid1 = "1";
        Organization org = await orgRepo.CreateOrg(orgname, userid1);

        var admins = await orgRepo.GetAdminsOfOrg(org.Id);

        Assert.That(admins.Contains(userid1), Is.True);
    }

    [Test]
    public async Task test_addremoveorgadmin()
    {
        string orgname = "test";
        string userid1 = "1";
        string userid2 = "2";
        Organization org = await orgRepo.CreateOrg(orgname, userid1);
        
        org = await orgRepo.AddAdmin(org.Id, userid2);
        Assert.That(org.Administrators.Contains(userid2), Is.True);

        org = await orgRepo.RemoveAdmin(org.Id, userid2);
        Assert.That(org.Administrators.Contains(userid2), Is.False);
    }

    [Test]
    public async Task test_orgcreatorisadmin()
    {
        string orgname = "test";
        string userid = "0";
        Organization org = await orgRepo.CreateOrg(orgname, userid);

        Assert.That(org.Administrators.Contains(userid), Is.True);
    }

    [Test]
    public async Task test_removeOrgOwnerFromAdminThrowsException()
    {
        string orgname = "test";
        string userid = "0";
        Organization org = await orgRepo.CreateOrg(orgname, userid);

        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.RemoveAdmin(org.Id, userid));
    }
    
    [Test]
    public async Task test_getorgmods()
    {
        string orgname = "test";
        string userid1 = "1";
        Organization org = await orgRepo.CreateOrg(orgname, userid1);

        var mods = await orgRepo.GetModsOfOrg(org.Id);

        Assert.That(mods.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task test_addremoveorgmod()
    {
        string orgname = "test";
        string userid1 = "1";
        Organization org = await orgRepo.CreateOrg(orgname, userid1);
        
        org = await orgRepo.AddModerator(org.Id, userid1);
        Assert.That(org.Moderators.Contains(userid1), Is.True);

        org = await orgRepo.RemoveModerator(org.Id, userid1);
        Assert.That(org.Moderators.Contains(userid1), Is.False);
    }
}