using HVZ.Persistence.Models;
using HVZ.Persistence.MongoDB.Repos;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using NodaTime;
namespace HVZ.Persistence.MongoDB.Tests;

[Parallelizable(ParallelScope.All)]
public class UserRepoTest : MongoTestBase
{
    public UserRepo CreateUserRepo() =>
        new UserRepo(CreateTemporaryDatabase(), Mock.Of<IClock>(), Mock.Of<ILogger>());

    [Test]
    public async Task create_then_read_are_equal()
    {
        UserRepo userRepo = CreateUserRepo();
        string userName = "ham";
        string userEmail = "ham@breakfast.club";

        User createdUser = await userRepo.CreateUser(userName, userEmail);
        User foundUser = await userRepo.Collection.Find(u => u.FullName == userName).FirstAsync();

        Assert.That(createdUser.Id, Is.Not.EqualTo(string.Empty));
        Assert.That(createdUser.Id, Is.EqualTo(foundUser.Id));
    }

    [Test]
    public async Task test_finduserbyid()
    {
        UserRepo userRepo = CreateUserRepo();
        string userName = "ham";
        string userEmail = "ham@breakfast.club";
        User createdUser = await userRepo.CreateUser(userName, userEmail);

        User? foundUser = await userRepo.FindUserById(createdUser.Id);
        User? notFoundUser = await userRepo.FindUserById(string.Empty);

        Assert.That(foundUser, Is.Not.Null);
        Assert.That(notFoundUser, Is.Null);
    }

    [Test]
    public async Task test_getuserbyid()
    {
        UserRepo userRepo = CreateUserRepo();
        string userName = "ham";
        string userEmail = "ham@breakfast.club";
        User createdUser = await userRepo.CreateUser(userName, userEmail);

        User foundUser = await userRepo.GetUserById(createdUser.Id);

        Assert.That(foundUser, Is.EqualTo(createdUser));
        Assert.ThrowsAsync<ArgumentException>(() => userRepo.GetUserById(string.Empty));
    }

    [Test]
    public async Task test_finduserbyname()
    {
        UserRepo userRepo = CreateUserRepo();
        string userName1 = "ham";
        string userEmail1 = "ham@breakfast.club";
        await userRepo.CreateUser(userName1, userEmail1);
        string userName2 = "hamilton";
        string userEmail2 = "hamilton@playhvz.org";
        await userRepo.CreateUser(userName2, userEmail2);

        User[] noUsers = await userRepo.FindUserByName("john");
        User[] oneUser = await userRepo.FindUserByName(userName2);
        User[] twoUsers = await userRepo.FindUserByName(userName1);

        Assert.That(noUsers.Length, Is.EqualTo(0));
        Assert.That(oneUser.Length, Is.EqualTo(1));
        Assert.That(twoUsers.Length, Is.EqualTo(2));
    }

    [Test]
    public async Task test_finduserbyemail()
    {
        UserRepo userRepo = CreateUserRepo();
        string userName = "bacon";
        string userEmail = "Bacon@bacon.bacon";
        User createdUser = await userRepo.CreateUser(userName, userEmail);

        User? foundUser = await userRepo.FindUserByEmail(userEmail);

        Assert.That(foundUser, Is.Not.Null);
        Assert.That(foundUser, Is.EqualTo(createdUser));

        //test case sensitive
        Assert.That(await userRepo.FindUserByEmail(userEmail.ToUpperInvariant()), Is.EqualTo(createdUser));
        Assert.That(await userRepo.FindUserByEmail(userEmail.ToLowerInvariant()), Is.EqualTo(createdUser));

        //test empty string passed
        Assert.That(await userRepo.FindUserByEmail(string.Empty), Is.Null);
    }

    [Test]
    public async Task test_getuserbyemail()
    {
        UserRepo userRepo = CreateUserRepo();
        string userName = "bacon";
        string userEmail = "Bacon@bacon.bacon";
        string unregisteredEmail = "bob@aol.com";
        User createdUser = await userRepo.CreateUser(userName, userEmail);

        User? foundUser = await userRepo.GetUserByEmail(userEmail);

        Assert.That(foundUser, Is.Not.Null);
        Assert.That(foundUser, Is.EqualTo(createdUser));

        Assert.Throws<ArgumentException>(() => userRepo.GetUserByEmail(unregisteredEmail).GetAwaiter().GetResult());
    }

    [Test]
    public async Task test_deleteUser()
    {
        UserRepo userRepo = CreateUserRepo();
        string userName = "karl";
        string userEmail = "karl@karl.com";
        User createdUser = await userRepo.CreateUser(userName, userEmail);

        User foundUser = await userRepo.GetUserById(createdUser.Id);

        await userRepo.DeleteUser(createdUser.Id);

        Assert.ThrowsAsync<ArgumentException>(() => userRepo.GetUserById(createdUser.Id));
    }

    [Test]
    public async Task test_setFullName()
    {
        UserRepo userRepo = CreateUserRepo();
        string userName = "karl";
        string updatedUserName = "User Name";
        string userEmail = "karl@karl.com";
        User createdUser = await userRepo.CreateUser(userName, userEmail);

        await userRepo.SetUserFullName(createdUser.Id, updatedUserName);

        User foundUser = await userRepo.GetUserById(createdUser.Id);

        Assert.That(foundUser.FullName, Is.EqualTo(updatedUserName));
    }

    [Test]
    public async Task test_getFullName()
    {
        UserRepo userRepo = CreateUserRepo();
        string userName = "karl";
        string userEmail = "karl@karl.com";
        User createdUser = await userRepo.CreateUser(userName, userEmail);

        string foundUserName = await userRepo.GetUserFullName(createdUser.Id);

        Assert.That(foundUserName, Is.EqualTo(userName));
    }
}