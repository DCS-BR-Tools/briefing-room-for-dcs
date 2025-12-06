using BriefingRoom4DCS.Data;

public class DatabaseFixture : IDisposable
{
    public DatabaseFixture()
    {
        Db = new Database();
    }

    public void Dispose()
    {
        // Clean up test data from the database
    }

    public IDatabase Db { get; private set; }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}