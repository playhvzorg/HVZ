namespace HVZ.Persistence.MongoDB.Tests;

using global::MongoDB.Driver;
using global::MongoDB.Driver.Linq;
using Serializers;

/// <summary>
///     Base class for tests that need to operate on an actual MongoDB server.
///     Connects to a local mongod instance running on the default port (27017).
///     Provides a CreateTemporaryDatabase method for obtaining a unique IMongoDatabase.
///     Databases created that way get cleaned up while the test class is being torn down.
/// </summary>
[Category("IntegrationTest")]
public abstract class MongoTestBase {
    private const string ReplicaSetName = "rs0";
    private static readonly Random Random = new();
    private readonly List<string> temporaryDatabases = new();

    private MongoClient client = null!;

    [OneTimeSetUp]
    public void SetUpMongoClient()
    {
        CustomSerializers.RegisterAll();
        // try to connect to a mongodb running on the default port
        MongoClientSettings settings = MongoClientSettings
            .FromConnectionString($"mongodb://localhost:27017/?replicaSet={ReplicaSetName}");
        settings.LinqProvider = LinqProvider.V3;
        client = new MongoClient(settings);
        bool success = client.ListDatabaseNamesAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(5));
        if (!success)
        {
            throw new AssertionException(
                "Failed to connect to a local MongoDB instance running on the default port. " +
                "Please start a local MongoDB instance on the default port (27017), " +
                $"and make sure it is in replica set mode with a replica set named '{ReplicaSetName}'. " +
                "Alternatively, skip these tests using 'dotnet test --filter TestCategory!=IntegrationTest'");
        }
    }

    [OneTimeTearDown]
    public void TearDownTempDatabases()
    {
        Task.WhenAll(temporaryDatabases.Select(db => client.DropDatabaseAsync(db))).Wait();
    }

    protected IMongoDatabase CreateTemporaryDatabase()
    {
        string dbName = "testdb-" + Random.Next();
        temporaryDatabases.Add(dbName);
        return client.GetDatabase(dbName);
    }
}