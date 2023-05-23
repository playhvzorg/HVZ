using NodaTime;

namespace HVZ.Tests.TestClasses
{
    public class TestData
    {
        public static readonly string testGameName = "Test Game";
        public static readonly string testGameId = "001";
        public static readonly string testAdminId = "1";
        public static readonly string testModId = "2";
        public static readonly string testUserId = "3";
        public static readonly string testOrgId = "01";
        public static readonly string testUserPlayerId = "0003";
        public static readonly string testModPlayerId = "0002";
        public static readonly string testAdminPlayerId = "0001";
        public static readonly string testUserFullName = "Test User";
        public static readonly string testUserEmail = "test@user.com";
        public static readonly Instant time = Instant.FromDateTimeOffset(
            new DateTimeOffset(
                month: 12,
                day: 12,
                year: 2023,
                hour: 0,
                minute: 0,
                second: 0,
                offset:
                TimeSpan.Zero));
    }
}
