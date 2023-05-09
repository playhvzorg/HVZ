using HVZ.Persistence.Models;
using HVZ.Persistence.MongoDB.Repos;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using NodaTime;

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
        orgRepo = new OrgRepo(CreateTemporaryDatabase(), Mock.Of<IClock>(), userRepoMock.Object, gameRepoMock.Object,
            Mock.Of<ILogger>());
    }

    [Test]
    public async Task create_then_read_are_equal()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";


        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization foundOrg = await orgRepo.Collection.Find(o => o.OwnerId == userid).FirstAsync();

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg.Id, Is.Not.EqualTo(String.Empty));
        Assert.That(createdOrg.Id, Is.Not.EqualTo(String.Empty));
    }

    [Test]
    public async Task test_findorgbyid()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization? foundOrg = await orgRepo.FindOrgById(createdOrg.Id);
        Organization? notFoundOrg = await orgRepo.FindOrgById(string.Empty);

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg, Is.Not.Null);
        Assert.That(notFoundOrg, Is.Null);
    }

    [Test]
    public async Task test_getorgbyid()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization foundOrg = await orgRepo.GetOrgById(createdOrg.Id);

        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.GetOrgById("000000000000000000000000"));
    }

    [Test]
    public async Task test_findorgbyname()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization? foundOrg = await orgRepo.FindOrgByName(orgname);
        Organization? notFoundOrg = await orgRepo.FindOrgByName(string.Empty);

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg, Is.Not.Null);
        Assert.That(notFoundOrg, Is.Null);
    }

    [Test]
    public async Task test_findorgbyurl()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization? foundOrg = await orgRepo.FindOrgByUrl(orgurl);
        Organization? notFoundOrg = await orgRepo.FindOrgByUrl(string.Empty);

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg, Is.Not.Null);
        Assert.That(notFoundOrg, Is.Null);
    }

    [Test]
    public async Task test_getorgbyurl()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization foundOrg = await orgRepo.GetOrgByUrl(orgurl);

        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.GetOrgByUrl("none"));
    }

    [Test]
    public async Task test_setactivegameoforg()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string gameid = "1";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(org.ActiveGameId, Is.Null);

        org = await orgRepo.SetActiveGameOfOrg(org.Id, gameid);

        Assert.That(org.ActiveGameId, Is.EqualTo(gameid));
    }

    [Test]
    public async Task test_getactivegameoforg()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string gameid = "1";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Game newGame = new ("test", gameid, userid, org.Id, Instant.MinValue, Game.GameStatus.New,
            Player.gameRole.Human, new HashSet<Player>(), new (), 9999);
        gameRepoMock.Setup(repo => repo.GetGameById("1")).ReturnsAsync(newGame);
        await orgRepo.SetActiveGameOfOrg(org.Id, gameid);

        Game? foundGame = await orgRepo.FindActiveGameOfOrg(org.Id);

        Assert.That(foundGame, Is.Not.Null);
        Assert.That(foundGame, Is.EqualTo(newGame));
    }

    [Test]
    public async Task test_removeactivegameoforg()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string gameid = "1";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Game newGame = new ("test", gameid, userid, org.Id, Instant.MinValue, Game.GameStatus.New,
            Player.gameRole.Human, new HashSet<Player>(), new (), 9999);
        gameRepoMock.Setup(repo => repo.GetGameById("1")).ReturnsAsync(newGame);
        await orgRepo.SetActiveGameOfOrg(org.Id, gameid);

        await orgRepo.RemoveActiveGameOfOrg(org.Id);

        Game? nullGame = await orgRepo.FindActiveGameOfOrg(org.Id);

        Assert.That(nullGame, Is.Null);
    }

    [Test]
    public async Task test_getorgadmins()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        var admins = await orgRepo.GetAdminsOfOrg(org.Id);

        Assert.That(admins.Contains(userid1), Is.True);
    }

    [Test]
    public async Task test_addremoveorgadmin()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        string userid2 = "2";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        org = await orgRepo.AddAdmin(org.Id, userid2);
        Assert.That(org.Administrators.Contains(userid2), Is.True);

        org = await orgRepo.RemoveAdmin(org.Id, userid2);
        Assert.That(org.Administrators.Contains(userid2), Is.False);
    }

    [Test]
    public async Task test_orgcreatorisadmin()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(org.Administrators.Contains(userid), Is.True);
    }

    [Test]
    public async Task test_removeOrgOwnerFromAdminThrowsException()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.RemoveAdmin(org.Id, userid));
    }

    [Test]
    public async Task test_getorgmods()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        var mods = await orgRepo.GetModsOfOrg(org.Id);

        Assert.That(mods.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task test_addremoveorgmod()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        org = await orgRepo.AddModerator(org.Id, userid1);
        Assert.That(org.Moderators.Contains(userid1), Is.True);

        org = await orgRepo.RemoveModerator(org.Id, userid1);
        Assert.That(org.Moderators.Contains(userid1), Is.False);
    }

    [Test]
    public async Task test_setorgowner()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        string userid2 = "2";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        //this should fail because userid2 is not an admin in the org
        Assert.ThrowsAsync<ArgumentException>(async () => await orgRepo.SetOwner(org.Id, userid2));

        await orgRepo.AddAdmin(org.Id, userid2);
        org = await orgRepo.SetOwner(org.Id, userid2);

        Assert.That(org.OwnerId, Is.EqualTo(userid2));
    }

    [Test]
    public async Task test_cant_create_new_game_while_active_game_exists()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "111111111111111111111111";
        string gameName = "testgame";
        string otherGameName = "othertestgame";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Game game = new Game(
            name: "gamename",
            gameid: "1",
            creatorid: userid,
            orgid: org.Id,
            createdat: Instant.MinValue,
            status: Game.GameStatus.New,
            defaultrole: Player.gameRole.Human,
            players: new HashSet<Player>(),
            eventLog: new (),
            maxOzTags: 9999
        );

        gameRepoMock.Setup(repo => repo.CreateGame(gameName, userid, org.Id, 3)).ReturnsAsync(game);
        gameRepoMock.Setup(repo => repo.GetGameById(game.Id)).ReturnsAsync(game);
        await orgRepo.CreateGame(gameName, userid, org.Id);
        org = await orgRepo.GetOrgById(org.Id);
        Assert.That(org.ActiveGameId, Is.Not.Null);
        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.CreateGame(otherGameName, userid, org.Id));
    }

    [Test]
    public async Task test_non_admin_cant_create_game()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "1";
        string otherUserId = "2";
        string gameName = "testgame";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Game game = new Game(
            name: "gamename",
            gameid: "1",
            creatorid: userid,
            orgid: org.Id,
            createdat: Instant.MinValue,
            status: Game.GameStatus.New,
            defaultrole: Player.gameRole.Human,
            players: new HashSet<Player>(),
            new (),
            maxOzTags: 9999
        );

        gameRepoMock.Setup(repo => repo.CreateGame(gameName, userid, org.Id, 3)).ReturnsAsync(game);

        Assert.That(await orgRepo.CreateGame(gameName, userid, org.Id), Is.Not.Null);
        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.CreateGame(gameName, otherUserId, org.Id));
    }

    [Test]
    public async Task test_orgadminsupdatedevent()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string newuserid = "1";
        Organization? eventOrg = null;

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        orgRepo.AdminsUpdated += delegate(object? sender, OrgUpdatedEventArgs args) { eventOrg = args.Org; };

        await orgRepo.AddAdmin(org.Id, newuserid);
        Assert.That(eventOrg, Is.Not.Null);

        eventOrg = null;

        await orgRepo.RemoveAdmin(org.Id, newuserid);
        Assert.That(eventOrg, Is.Not.Null);
    }

    [Test]
    public async Task test_orgmodsupdatedevent()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string newuserid = "1";
        Organization? eventOrg = null;

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        orgRepo.ModsUpdated += delegate(object? sender, OrgUpdatedEventArgs args) { eventOrg = args.Org; };

        await orgRepo.AddModerator(org.Id, newuserid);
        Assert.That(eventOrg, Is.Not.Null);

        eventOrg = null;

        await orgRepo.RemoveModerator(org.Id, newuserid);
        Assert.That(eventOrg, Is.Not.Null);
    }

    [Test]
    public async Task test_orgdescription()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string orgdesc = "description";
        string userid = "0";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(org.Description, Is.EqualTo(string.Empty));

        org = await orgRepo.SetOrgDescription(org.Id, orgdesc);

        Assert.That(org.Description, Is.EqualTo(orgdesc));
    }

    [Test]
    public async Task test_getorgdescription()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string orgdesc = "description";
        string userid = "0";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(await orgRepo.GetOrgDescription(org.Id), Is.EqualTo(string.Empty));

        org = await orgRepo.SetOrgDescription(org.Id, orgdesc);

        Assert.That(await orgRepo.GetOrgDescription(org.Id), Is.EqualTo(orgdesc));
    }

    [Test]
    public async Task test_descriptionsettingsupdatedevent()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string newdescription = "org description";
        Organization? eventOrg = null;

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        orgRepo.SettingsUpdated += delegate(object? sender, OrgUpdatedEventArgs args) { eventOrg = args.Org; };

        await orgRepo.SetOrgDescription(org.Id, newdescription);
        Assert.That(eventOrg, Is.Not.Null);
    }

    [Test]
    public async Task test_setorgname()
    {
        string orgname = "test";
        string neworgname = "cooler test";
        string orgurl = "testurl";
        string userid = "0";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(org.Name, Is.EqualTo(orgname));

        await orgRepo.SetOrgName(org.Id, neworgname);

        org = await orgRepo.GetOrgById(org.Id);

        Assert.That(org.Name, Is.EqualTo(neworgname));
    }

    [Test]
    public async Task test_getorgname()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(await orgRepo.GetOrgName(org.Id), Is.EqualTo(orgname));
    }

    [Test]
    public async Task test_setnamesettingsupdatedevent()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string newname = "cooler org";
        Organization? eventOrg = null;

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        orgRepo.SettingsUpdated += delegate(object? sender, OrgUpdatedEventArgs args) { eventOrg = args.Org; };

        await orgRepo.SetOrgName(org.Id, newname);
        Assert.That(eventOrg, Is.Not.Null);
    }

    [Test]
    public async Task test_setorgrequireverifiedemail()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(org.RequireVerifiedEmailForPlayer, Is.False);

        await orgRepo.SetRequireVerifiedEmail(org.Id, true);

        org = await orgRepo.GetOrgById(org.Id);

        Assert.That(org.RequireVerifiedEmailForPlayer, Is.True);
    }

    [Test]
    public async Task test_getrequireverifiedemail()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(await orgRepo.GetRequireVerifiedEmail(org.Id), Is.False);

        await orgRepo.SetRequireVerifiedEmail(org.Id, true);

        Assert.That(await orgRepo.GetRequireVerifiedEmail(org.Id), Is.True);
    }

    [Test]
    public async Task test_requireemailsettingsupdatedevent()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        Organization? eventOrg = null;

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        orgRepo.SettingsUpdated += delegate(object? sender, OrgUpdatedEventArgs args) { eventOrg = args.Org; };

        await orgRepo.SetRequireVerifiedEmail(org.Id, true);
        Assert.That(eventOrg, Is.Not.Null);
    }

    [Test]
    public async Task test_setrequireprofilepicture()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(org.RequireProfilePictureForPlayer, Is.False);

        await orgRepo.SetRequireProfilePicture(org.Id, true);

        org = await orgRepo.GetOrgById(org.Id);

        Assert.That(org.RequireProfilePictureForPlayer, Is.True);
    }

    [Test]
    public async Task test_getrequiredprofilepicture()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(await orgRepo.GetRequireProfilePicture(org.Id), Is.False);

        await orgRepo.SetRequireProfilePicture(org.Id, true);

        Assert.That(await orgRepo.GetRequireProfilePicture(org.Id), Is.True);
    }

    [Test]
    public async Task test_requireprofilepicturesettingsupdatedevent()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        Organization? eventOrg = null;

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        orgRepo.SettingsUpdated += delegate(object? sender, OrgUpdatedEventArgs args) { eventOrg = args.Org; };

        await orgRepo.SetRequireProfilePicture(org.Id, true);
        Assert.That(eventOrg, Is.Not.Null);
    }

    [Test]
    public async Task test_setorgdiscordserverid()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string discordid = "12345";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(org.DiscordServerId, Is.Null);

        await orgRepo.SetOrgDiscordServerId(org.Id, discordid);

        org = await orgRepo.GetOrgById(org.Id);

        Assert.That(org.DiscordServerId, Is.EqualTo(discordid));
    }

    [Test]
    public async Task test_getorgdiscordserverid()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string discordid = "12345";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.ThrowsAsync<ArgumentException>(async () => await orgRepo.GetOrgDiscordServerId(org.Id));

        await orgRepo.SetOrgDiscordServerId(org.Id, discordid);

        Assert.That(await orgRepo.GetOrgDiscordServerId(org.Id), Is.EqualTo(discordid));
    }
}