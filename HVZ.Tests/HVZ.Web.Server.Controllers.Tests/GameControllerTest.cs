using HVZ.Persistence;
using HVZ.Persistence.Models;
using HVZ.Tests.TestClasses;
using HVZ.Web.Server.Controllers;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Security.Principal;

namespace HVZ.Tests.HVZ.Web.Server.Controllers.Tests
{
    public class GameControllerTest
    {
        private GameController _gameController;
        private readonly Mock<IGameRepo> _mockGameRepo = new();
        private readonly Mock<IUserRepo> _mockUserRepo = new();
        private readonly Mock<IOrgRepo> _mockOrgRepo = new();
        private Mock<HttpContext> _context = new();

        private ClaimsPrincipal UnauthenticatedUser = Mock.Of<ClaimsPrincipal>(
            u => u.Identity == Mock.Of<IIdentity>(
                i => i.IsAuthenticated == false));

        private ClaimsPrincipal AuthenticatedUser = Mock.Of<ClaimsPrincipal>(
            u => u.Identity == Mock.Of<IIdentity>(
                i => i.IsAuthenticated == true) &&
            u.Claims == new List<Claim>
            {
                new Claim("DatabaseId", TestData.testUserId)
            });

        private ClaimsPrincipal AuthenticatedMod = Mock.Of<ClaimsPrincipal>(
            u => u.Identity == Mock.Of<IIdentity>(
                i => i.IsAuthenticated == true) &&
            u.Claims == new List<Claim>
            {
                new Claim("DatabaseId", TestData.testModId)
            });

        private readonly HashSet<Player> testPlayerList = new()
        {
            new Player(
                userid: TestData.testUserId,
                gameId: TestData.testUserPlayerId,
                role: Player.gameRole.Human,
                tags: 0,
                joinedGameAt: TestData.time),
            new Player(
                userid: TestData.testModId,
                gameId: TestData.testModPlayerId,
                role: Player.gameRole.Zombie,
                tags: 1,
                joinedGameAt: TestData.time),
            new Player(
                userid: TestData.testAdminId,
                gameId: TestData.testAdminPlayerId,
                role: Player.gameRole.Oz,
                tags: 2,
                joinedGameAt: TestData.time)
        };

        private readonly List<GameEventLog> testEventLog = new()
        {
            new GameEventLog( // Game created
                GameEvent.GameCreated,
                TestData.time,
                TestData.testAdminId,
                new Dictionary<string, object>
                {
                    { "name", TestData.testGameName }
                }),
            new GameEventLog( // Game started
                GameEvent.GameStarted,
                TestData.time,
                TestData.testAdminId),
            new GameEventLog( // Set player to OZ
                GameEvent.PlayerRoleChangedByMod,
                TestData.time,
                TestData.testUserId,
                new Dictionary<string, object>()
                {
                    { "modid", "ozmaxtagsreached" },
                    { "role", Player.gameRole.Oz }
                }),
            new GameEventLog( // Oz tag
                GameEvent.Tag,
                TestData.time,
                TestData.testUserId,
                new Dictionary<string, object>()
                {
                    { "tagreciever", TestData.testModId },
                    { "taggertagcount", 1 },
                    { "oztagger", true }
                }),
            new GameEventLog( // Normal tag
                GameEvent.Tag,
                TestData.time,
                TestData.testModId,
                new Dictionary<string, object>()
                {
                    { "tagreciever", TestData.testAdminId },
                    { "taggertagcount", 1 },
                    { "oztagger", false }
                }),
            new GameEventLog( // Oz chooses role
                GameEvent.PlayerRoleChangedByMod,
                TestData.time,
                TestData.testUserId,
                new Dictionary<string, object>()
                {
                    { "modid", "ozmaxtagsreached" },
                    { "role", Player.gameRole.Human },
                }),
            new GameEventLog( // Set to human
                GameEvent.PlayerRoleChangedByMod,
                TestData.time,
                TestData.testModId,
                new Dictionary<string, object>()
                {
                    { "modid", TestData.testModId },
                    { "role", Player.gameRole.Human }
                })
        };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _mockGameRepo.Setup(r => r.FindGameById(TestData.testGameId))
                .ReturnsAsync(Mock.Of<TestGame>(g =>
                    g.EventLog == testEventLog &&
                    g.Players == testPlayerList));

            _mockOrgRepo.Setup(r => r.IsModOfOrg(TestData.testOrgId, TestData.testModId))
                .ReturnsAsync(true);

            _mockOrgRepo.Setup(r => r.IsModOfOrg(TestData.testOrgId, TestData.testUserId))
                .ReturnsAsync(false);

            _gameController = new GameController(
                Mock.Of<ILogger<GameController>>(),
                _mockGameRepo.Object,
                _mockOrgRepo.Object,
                _mockUserRepo.Object);

            _gameController.ControllerContext = new ControllerContext(
                new ActionContext(
                    _context.Object, new RouteData(), new ControllerActionDescriptor()));
        }

        #region GetConfig Tests

        [Test]
        public async Task Test_GetConfig_Unauthenticated_User_Forbidden()
        {
            _context.Setup(c => c.User).Returns(UnauthenticatedUser);

            var result = await _gameController.GetGameConfig("1234");
            var config = result.Result as OkObjectResult;
            Assert.That(config, Is.Null);

            var forbid = result.Result as ForbidResult;
            Assert.That(forbid, Is.Not.Null);
        }

        [Test]
        public async Task Test_GetConfig_Returns_Nonmod_Unauthorized()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);

            var result = await _gameController.GetGameConfig(TestData.testGameId);
            var config = result.Result as OkObjectResult;
            Assert.That(config, Is.Null);

            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);
            Assert.That(unauthorized.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task Test_GetConfig_Returns_Mod_Ok()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedMod);

            var result = await _gameController.GetGameConfig(TestData.testGameId);
            var config = result.Result as OkObjectResult;
            Assert.That(config, Is.Not.Null);

            var configObject = config.Value as GameConfig;
            Assert.That(configObject, Is.Not.Null);
        }

        [Test]
        public async Task Test_GetConfig_Returns_NotFound_InvalidId()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedMod);

            _gameController.ControllerContext = new ControllerContext(
                new ActionContext(
                    _context.Object, new RouteData(), new ControllerActionDescriptor()));

            var result = await _gameController.GetGameConfig(TestData.testOrgId);
            var config = result.Result as OkObjectResult;
            Assert.That(config, Is.Null);

            var notFound = result.Result as NotFoundObjectResult;
            Assert.That(notFound, Is.Not.Null);
            Assert.That(notFound.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        #endregion

        #region Tag Tests

        [Test]
        public async Task Test_Tag_Unauthenticated_User_Forbidden()
        {
            _context.Setup(c => c.User).Returns(UnauthenticatedUser);

            _gameController.ControllerContext = new ControllerContext(
                new ActionContext(
                    _context.Object, new RouteData(), new ControllerActionDescriptor()));

            var result = await _gameController.Tag(TestData.testGameId, new TagModel
            {
                ReceiverGameId = TestData.testModId
            });

            var tagResult = result.Result as OkObjectResult;
            Assert.That(tagResult, Is.Null);

            var forbid = result.Result as ForbidResult;
            Assert.That(forbid, Is.Not.Null);
        }

        [Test]
        public async Task Test_Tag_Returns_NotFound_InvalidId()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);

            var result = await _gameController.Tag(TestData.testOrgId, new TagModel
            {
                ReceiverGameId = TestData.testModId
            });

            var notFoundResult = result.Result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);

            var tagResult = (TagResult)notFoundResult.Value!;
            Assert.That(tagResult.Succeeded, Is.False);
            Assert.That(tagResult.Error, Is.Not.Null);
        }

        [Test]
        public async Task Test_Tag_PlayerNotInGame_Unauthorized()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);
            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync((Player?)null);

            var result = await _gameController.Tag(TestData.testGameId, new TagModel
            {
                ReceiverGameId = TestData.testModId
            });

            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);

            var tagResult = (TagResult)unauthorized.Value!;
            Assert.That(tagResult.Succeeded, Is.False);
            Assert.That(tagResult.Error, Is.Not.Null);
        }

        [Test]
        public async Task Test_Tag_HumanTagger_Unauthorized()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);
            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Human));

            var result = await _gameController.Tag(TestData.testGameId, new TagModel
            {
                ReceiverGameId = TestData.testModId
            });

            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);

            var tagResult = (TagResult)unauthorized.Value!;
            Assert.That(tagResult.Succeeded, Is.False);
            Assert.That(tagResult.Error, Is.Not.Null);
        }

        [Test]
        public async Task Test_Tag_OzTagger_TooManyTags_Unauthorized()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);
            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Oz &&
                p.Tags == Mock.Of<TestGame>().OzMaxTags));

            var result = await _gameController.Tag(TestData.testGameId, new TagModel
            {
                ReceiverGameId = TestData.testModId
            });

            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);

            var tagResult = (TagResult)unauthorized.Value!;
            Assert.That(tagResult.Succeeded, Is.False);
            Assert.That(tagResult.Error, Is.Not.Null);
        }

        [Test]
        public async Task Test_Tag_ReceiverNotFound_NotFound()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);
            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Zombie));

            var result = await _gameController.Tag(TestData.testGameId, new TagModel
            {
                ReceiverGameId = TestData.testModId
            });

            var notFound = result.Result as NotFoundObjectResult;
            Assert.That(notFound, Is.Not.Null);

            var tagResult = (TagResult)notFound.Value!;
            Assert.That(tagResult.Succeeded, Is.False);
            Assert.That(tagResult.Error, Is.Not.Null);
        }

        [Test]
        public async Task Test_Tag_ReceiverNotHuman_Unauthorized()
        {


            _context.Setup(c => c.User).Returns(AuthenticatedUser);
            _context.Setup(c => c.Request).Returns(Mock.Of<HttpRequest>());

            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Zombie));

            _mockGameRepo.Setup(r => r.FindPlayerByGameId(TestData.testGameId, TestData.testModPlayerId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Zombie &&
                p.GameId == TestData.testModPlayerId));

            var result = await _gameController.Tag(TestData.testGameId, new TagModel
            {
                ReceiverGameId = TestData.testModPlayerId
            });

            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);

            var tagResult = (TagResult)unauthorized.Value!;
            Assert.That(tagResult.Succeeded, Is.False);
            Assert.That(tagResult.Error, Is.Not.Null);
        }

        [Test]
        public async Task Test_Tag_Success_ZombieTagger_Ok()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);

            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Zombie));

            _mockGameRepo.Setup(r => r.FindPlayerByGameId(TestData.testGameId, TestData.testModPlayerId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Human &&
                p.GameId == TestData.testModPlayerId &&
                p.UserId == TestData.testModId));

            var result = await _gameController.Tag(TestData.testGameId, new TagModel
            {
                ReceiverGameId = TestData.testModPlayerId
            });

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var tagResult = (TagResult)ok.Value!;
            Assert.That(tagResult.Succeeded, Is.True);
            Assert.That(tagResult.Tags, Is.EqualTo(Mock.Of<TestPlayer>().Tags + 1));
            Assert.That(tagResult.ReceiverUserId, Is.EqualTo(TestData.testModId));
            Assert.That(tagResult.OzTags, Is.Null);
        }

        [Test]
        public async Task Test_Tag_Success_OzTagger_Ok()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);

            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Oz));

            _mockGameRepo.Setup(r => r.FindPlayerByGameId(TestData.testGameId, TestData.testModPlayerId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Human &&
                p.GameId == TestData.testModPlayerId &&
                p.UserId == TestData.testModId));

            var result = await _gameController.Tag(TestData.testGameId, new TagModel
            {
                ReceiverGameId = TestData.testModPlayerId
            });

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var tagResult = (TagResult)ok.Value!;
            Assert.That(tagResult.Succeeded, Is.True);
            Assert.That(tagResult.Tags, Is.EqualTo(Mock.Of<TestPlayer>().Tags + 1));
            Assert.That(tagResult.ReceiverUserId, Is.EqualTo(TestData.testModId));
            Assert.That(tagResult.OzTags, Is.Not.Null);
        }

        #endregion

        #region GameInfo Tests

        [Test]
        public async Task Test_GameInfo_InvalidId_NotFound()
        {
            var result = await _gameController.GetGameInfo(TestData.testOrgId);

            var notFound = result.Result as NotFoundObjectResult;
            Assert.That(notFound, Is.Not.Null);

            var notFoundError = (string?)notFound.Value;
            Assert.That(notFoundError, Is.Not.Null);
        }

        [Test]
        public async Task Test_GameInfo_Ok()
        {
            var result = await _gameController.GetGameInfo(TestData.testGameId);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var gameInfo = (GameInfo?)ok.Value;
            Assert.That(gameInfo, Is.Not.Null);
            Assert.That(gameInfo?.Name, Is.EqualTo(TestData.testGameName));
        }

        #endregion

        #region EventLog Tests

        // TODO: Add tests
        [Test]
        public async Task Test_GameLog_NotFound()
        {
            var result = await _gameController.GetEventLog(TestData.testOrgId);

            var notFound = result.Result as NotFoundObjectResult;
            Assert.That(notFound, Is.Not.Null);

            var notFoundError = (string?)notFound.Value;
            Assert.That(notFoundError, Is.Not.Null);
        }

        [Test]
        public async Task Test_GameLog_Unauthenticated_RemoveOz()
        {
            _context.Setup(c => c.User).Returns(UnauthenticatedUser);

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestUser>());

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testModId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testModId));

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testAdminId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testAdminId));

            var result = await _gameController.GetEventLog(TestData.testGameId);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var eventLog = (IEnumerable<GameLogData>)ok.Value!;
            Assert.That(eventLog, Is.Not.Null);
            Assert.That(eventLog.Count, Is.EqualTo(5));

            // TODO: OZ tag is modified
        }

        [Test]
        public async Task Test_GameLog_AuthenticatedMod_NoRemoveOz()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedMod);

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestUser>());

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testModId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testModId));

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testAdminId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testAdminId));

            var result = await _gameController.GetEventLog(TestData.testGameId);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var eventLog = (IEnumerable<GameLogData>)ok.Value!;
            Assert.That(eventLog, Is.Not.Null);
            Assert.That(eventLog.Count, Is.EqualTo(testEventLog.Count));

            // TODO: OZ tag is unmodified
        }

        [Test]
        public async Task Test_GameLog_AuthenticatedOz_NoRemoveOz()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestUser>());

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testModId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testModId));

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testAdminId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testAdminId));

            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Oz));

            var result = await _gameController.GetEventLog(TestData.testGameId);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var eventLog = (IEnumerable<GameLogData>)ok.Value!;
            Assert.That(eventLog, Is.Not.Null);
            Assert.That(eventLog.Count, Is.EqualTo(testEventLog.Count));
        }

        #endregion

        #region Player List Tests

        [Test]
        public async Task Test_PlayerList_Notfound()
        {
            var result = await _gameController.GetPlayers(TestData.testOrgId);

            var notFound = result.Result as NotFoundObjectResult;
            Assert.That(notFound, Is.Not.Null);

            var error = (string?)notFound.Value;
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public async Task Test_PlayerList_Unauthenticated_HideOz()
        {
            _context.Setup(c => c.User).Returns(UnauthenticatedUser);

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestUser>());

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testModId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testModId));

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testAdminId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testAdminId));

            var result = await _gameController.GetPlayers(TestData.testGameId);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var playersList = (IEnumerable<PlayerData>)ok.Value!;
            Assert.That(playersList.Where(p => p.Role == Player.gameRole.Human).Count, Is.EqualTo(2));
            Assert.That(playersList.Where(p => p.Tags == 0).Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Test_PlayerList_AuthenticatedNotOz_HideOz()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);

            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Human));

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestUser>());

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testModId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testModId));

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testAdminId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testAdminId));
            var result = await _gameController.GetPlayers(TestData.testGameId);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var playersList = (IEnumerable<PlayerData>)ok.Value!;
            Assert.That(playersList.Where(p => p.Role == Player.gameRole.Human).Count, Is.EqualTo(2));
            Assert.That(playersList.Where(p => p.Tags == 0).Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Test_PlayerList_AuthenticatedOz_ShowOz()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedUser);

            _mockGameRepo.Setup(r => r.FindPlayerByUserId(TestData.testGameId, TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestPlayer>(p => p.Role == Player.gameRole.Oz));

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestUser>());

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testModId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testModId));

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testAdminId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testAdminId));
            var result = await _gameController.GetPlayers(TestData.testGameId);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var playersList = (IEnumerable<PlayerData>)ok.Value!;
            Assert.That(playersList.Where(p => p.Role == Player.gameRole.Human).Count, Is.EqualTo(1));
            Assert.That(playersList.Where(p => p.Tags == 0).Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_PlayerList_AuthenticatedMod_ShowOz()
        {
            _context.Setup(c => c.User).Returns(AuthenticatedMod);

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testUserId))
                .ReturnsAsync(Mock.Of<TestUser>());

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testModId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testModId));

            _mockUserRepo.Setup(r => r.GetUserById(TestData.testAdminId))
                .ReturnsAsync(Mock.Of<TestUser>(u => u.Id == TestData.testAdminId));
            var result = await _gameController.GetPlayers(TestData.testGameId);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var playersList = (IEnumerable<PlayerData>)ok.Value!;
            Assert.That(playersList.Where(p => p.Role == Player.gameRole.Human).Count, Is.EqualTo(1));
            Assert.That(playersList.Where(p => p.Tags == 0).Count, Is.EqualTo(1));
        }

        #endregion

    }
}
