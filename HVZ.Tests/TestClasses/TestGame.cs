using HVZ.Persistence.Models;

namespace HVZ.Tests.TestClasses
{
    public class TestGame : Game
    {
        public TestGame() :
            base(
                name: TestData.testGameName,
                gameid: TestData.testGameId,
                creatorid: TestData.testAdminId,
                orgid: TestData.testOrgId,
                createdat: TestData.time,
                status: Game.GameStatus.New,
                defaultrole: Player.gameRole.Human,
                players: new HashSet<Player>(),
                eventLog: new List<GameEventLog>(),
                maxOzTags: 3)
        {

        }
    }
}
