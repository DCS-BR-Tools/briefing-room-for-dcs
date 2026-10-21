using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Tests;

public sealed class LocalAircraftFilesTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"BriefingRoom-local-{Guid.NewGuid():N}");
    private readonly Database database = new();

    public LocalAircraftFilesTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "Liveries"));
        Directory.CreateDirectory(Path.Combine(root, "MissionEditor", "UnitPayloads"));
        Assert.True(database.SetDCSSaveGamePath(root));
    }

    [Fact]
    public void ImportsExactNameLiveriesWithoutDuplicates()
    {
        AddLivery("TEST", "Custom");
        File.WriteAllText(Path.Combine(root, "Liveries", "TEST", "Archived.zip"), "");
        var aircraft = CreateAircraft("TEST");

        aircraft.GetDCSLiveries(database);
        aircraft.GetDCSLiveries(database);

        Assert.Equal(2, aircraft.Liveries[Country.ALL].Count);
        Assert.Contains(Tuple.Create("Custom", "Custom"), aircraft.Liveries[Country.ALL]);
        Assert.Contains(Tuple.Create("Archived", "Archived"), aircraft.Liveries[Country.ALL]);
    }

    [Fact]
    public void ImportsExactNamePayload()
    {
        AddPayload("TEST", "TEST");
        var aircraft = CreateAircraft("TEST");

        aircraft.GetDCSPayloads(database);

        Assert.Equal("Custom", Assert.Single(aircraft.Payloads).name);
    }

    [Fact]
    public void ImportsA10AliasAndExactFolderTogether()
    {
        AddLivery("A-10CII", "Alias");
        AddLivery("A-10C_2", "Exact");
        AddLivery("A-10C", "Other aircraft");
        var aircraft = CreateAircraft("A-10C_2");
        aircraft.GetDCSLiveries(database);
        Assert.Equal(2, aircraft.Liveries[Country.ALL].Count);
        Assert.Contains(Tuple.Create("Alias", "Alias"), aircraft.Liveries[Country.ALL]);
        Assert.Contains(Tuple.Create("Exact", "Exact"), aircraft.Liveries[Country.ALL]);
    }

    [Theory]
    [InlineData("Renamed", "A-10C_2")]
    [InlineData("A-10CII", "a-10c_2")]
    public void ImportsPayloadByDeclaredUnitType(string filename, string unitType)
    {
        AddPayload(filename, unitType);
        var aircraft = CreateAircraft("A-10C_2");
        aircraft.GetDCSPayloads(database);
        Assert.Single(aircraft.Payloads);
        var originalName = CreateAircraft(filename);
        originalName.GetDCSPayloads(database);
        Assert.Single(originalName.Payloads);
    }

    [Fact]
    public void LiveryAliasRetainsOriginalFolderLookup()
    {
        AddLivery("A-10CII", "Custom");
        var index = new LocalAircraftFiles(root);
        Assert.Equal(
            Assert.Single(index.GetLiveryFolders("A-10C_2")),
            Assert.Single(index.GetLiveryFolders("A-10CII")));
    }

    [Fact]
    public void MatchingFilenameAndMetadataImportPayloadOnlyOnceIgnoringCase()
    {
        AddPayload("TEST", "test");
        var aircraft = CreateAircraft("TEST");
        aircraft.GetDCSPayloads(database);
        Assert.Single(aircraft.Payloads);
    }

    [Fact]
    public void ImportsBothFilenameAndMetadataMatchesTogether()
    {
        AddPayload("TEST", "OTHER");
        AddPayload("Renamed", "TEST");
        var aircraft = CreateAircraft("TEST");
        aircraft.GetDCSPayloads(database);
        Assert.Equal(2, aircraft.Payloads.Count);
    }

    [Fact]
    public void PayloadWithoutUnitTypeUsesFilename()
    {
        AddPayload("TEST", "TEST");
        var file = Path.Combine(root, "MissionEditor", "UnitPayloads", "TEST.lua");
        File.WriteAllText(file, File.ReadAllText(file).Replace("""["unitType"] = "TEST",""", ""));
        var aircraft = CreateAircraft("TEST");
        aircraft.GetDCSPayloads(database);
        Assert.Single(aircraft.Payloads);
    }

    [Fact]
    public void SharedIndexImportsMultiplePayloadFilesForOneAircraft()
    {
        AddPayload("First", "TEST");
        AddPayload("Second", "TEST");
        AddPayload("Unrelated", "OTHER");
        AddLivery("TEST", "Custom");
        var index = new LocalAircraftFiles(root);
        var aircraft = CreateAircraft("TEST");

        aircraft.GetDCSPayloads(database, index);
        aircraft.GetDCSLiveries(database, index);

        Assert.Equal(2, aircraft.Payloads.Count);
        Assert.Single(aircraft.Liveries[Country.ALL]);
    }

    [Fact]
    public void LiveryFolderMatchingIsCaseInsensitive()
    {
        AddLivery("a-10cii", "Custom");
        var aircraft = CreateAircraft("A-10C_2");
        aircraft.GetDCSLiveries(database);
        Assert.Single(aircraft.Liveries[Country.ALL]);
    }

    [Fact]
    public void MissingAircraftFoldersLeaveBuiltInDataUntouched()
    {
        var aircraft = CreateAircraft("MISSING");
        aircraft.GetDCSLiveries(database);
        aircraft.GetDCSPayloads(database);
        Assert.Empty(aircraft.Liveries);
        Assert.Empty(aircraft.Payloads);
    }

    private static DBEntryAircraft CreateAircraft(string id) => new()
    {
        DCSID = id,
        Liveries = new(),
        Payloads = new()
    };

    private void AddLivery(string aircraft, string name)
    {
        var path = Path.Combine(root, "Liveries", aircraft, name);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "description.lua"), "");
    }

    private void AddPayload(string filename, string unitType)
    {
        File.WriteAllText(Path.Combine(root, "MissionEditor", "UnitPayloads", $"{filename}.lua"),
            $$"""
            local unitPayloads = {
                ["unitType"] = "{{unitType}}",
                ["payloads"] = {
                    [1] = {
                        ["name"] = "Custom",
                        ["tasks"] = { [1] = 31 },
                        ["pylons"] = {}
                    }
                }
            }
            return unitPayloads
            """);
    }

    public void Dispose() => Directory.Delete(root, true);
}
