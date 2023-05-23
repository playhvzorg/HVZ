using HVZ.Persistence.Models;

namespace HVZ.Tests.TestClasses
{
    public class TestUser : User
    {
        public TestUser() :
            base(
                id: TestData.testUserId,
                fullName: TestData.testUserFullName,
                email: TestData.testUserEmail,
                createdAt: TestData.time)
        {

        }
    }
}
