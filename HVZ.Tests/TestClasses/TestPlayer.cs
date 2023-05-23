using HVZ.Persistence.Models;

namespace HVZ.Tests.TestClasses
{
    public class TestPlayer : Player
    {
        public TestPlayer() :
            base(
                userid: TestData.testUserId,
                gameId: TestData.testUserPlayerId,
                role: gameRole.Human,
                tags: 0,
                joinedGameAt: TestData.time)
        {

        }
    }
}
