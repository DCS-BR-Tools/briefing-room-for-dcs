using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Tests;

// The hint-map JS in CommonGUI/wwwroot/js/map.js routes airbase markers and zone
// polygons into per-side Leaflet layer groups by string-matching the keys it
// receives from BriefingRoom.GetMapSupportingMapData (DrawingMaker.GetPreviewMapData).
// If those prefixes drift on the C# side, the planner toggles silently stop
// filtering. These tests lock the contract so a rename on either side trips CI.
[Collection("Database collection")]
public class MapDataContractTests
{
    DatabaseFixture fixture;

    public MapDataContractTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    static readonly string[] PlannerJsKeyPrefixes =
    {
        "Blue_AIRBASE",
        "Enemy_AIRBASE",
        "Neutral_AIRBASE",
        "PLAYER_AIRBASE",
        "BLUE",
        "RED",
    };

    [Fact]
    public void PreviewMapDataKeysHonorPlannerJsContract()
    {
        var briefingRoom = new BriefingRoom(fixture.Db);
        var template = new MissionTemplate(
            fixture.Db,
            $"{BriefingRoom.GetBriefingRoomRootPath()}\\Default.brt");

        var mapData = briefingRoom.GetMapSupportingMapData(template);

        Assert.NotEmpty(mapData);
        foreach (var key in mapData.Keys)
        {
            Assert.True(
                PlannerJsKeyPrefixes.Any(prefix => key.StartsWith(prefix)),
                $"Map data key '{key}' does not match any prefix the planner JS routes on. " +
                $"Either update CommonGUI/wwwroot/js/map.js (GetHintAirbaseSide / GetHintZoneSide) " +
                $"to handle the new prefix, or update PlannerJsKeyPrefixes here.");
        }

        Assert.Contains(mapData.Keys, k =>
            k.StartsWith("Blue_AIRBASE") ||
            k.StartsWith("Enemy_AIRBASE") ||
            k.StartsWith("Neutral_AIRBASE"));
    }
}
