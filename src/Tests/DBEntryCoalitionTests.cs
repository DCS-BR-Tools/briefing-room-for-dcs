using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class DBEntryCoalitionTests
{
    private readonly DatabaseFixture fixture;

    public DBEntryCoalitionTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public void ShareArmsDefaultsToTrueWhenNotSpecified()
    {
        var entry = LoadCoalitionFromIni("""
[GUI]
DisplayName=Test Coalition
Category=Test

[Coalition]
Countries=USA
DefaultUnitList=FirstWorld
""");

        Assert.True(entry.ShareArms);
    }

    [Fact]
    public void ShareArmsCanBeDisabledInCoalitionIni()
    {
        var entry = LoadCoalitionFromIni("""
[GUI]
DisplayName=Test Coalition
Category=Test

[Coalition]
Countries=USA
DefaultUnitList=FirstWorld
ShareArms=False
""");

        Assert.False(entry.ShareArms);
    }

    private DBEntryCoalition LoadCoalitionFromIni(string iniText)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"br-coalition-{Guid.NewGuid():N}.ini");
        try
        {
            File.WriteAllText(tempFile, iniText);
            var entry = new DBEntryCoalition();
            var loaded = entry.Load(fixture.Db, $"TestCoalition{Guid.NewGuid():N}", tempFile);
            Assert.True(loaded);
            return entry;
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
