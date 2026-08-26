using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class OperationalDataWarningsTests
{
    private readonly DatabaseFixture fixture;

    public OperationalDataWarningsTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public void PlaneParsingWarnsWhenOperationalDataIsEmpty()
    {
        using var tempFiles = new JsonTestFiles(
            "UnitPlanes",
            """
            [
              {
                "type":"TEST_PLANE",
                "Operators":{},
                "countriesWorldID":[],
                "displayName":"Test Plane",
                "module":"",
                "shape":"test-plane-shape",
                "detectionRange":0,
                "threatRangeMin":0,
                "threatRange":0,
                "paintSchemes":{},
                "payloadPresets":[],
                "tasks":[],
                "fuel":1000,
                "flares":0,
                "chaff":0,
                "radio":{"frequency":251.0,"modulation":0},
                "maxAlt":10000,
                "cruiseSpeed":250,
                "panelRadio":[],
                "extraProps":[],
                "EPLRS":false,
                "ammoType":null,
                "height":1,
                "width":1,
                "length":1,
                "callsigns":{}
              }
            ]
            """,
            """
            [
              {
                "type":"TEST_PLANE",
                "families":["PlaneFighter"],
                "lowPolly":false,
                "immovable":false,
                "playerControllable":true
              }
            ]
            """);

        var messages = CaptureWarnings(() => DBEntryAircraft.LoadJSON(fixture.Db, tempFiles.DataFilePath, fixture.Db.Language));
        Assert.Contains(messages, x => x.Contains("\"TEST_PLANE\" operational data is empty.", StringComparison.Ordinal));
    }

    [Fact]
    public void HelicopterParsingWarnsWhenOperationalDataIsEmpty()
    {
        using var tempFiles = new JsonTestFiles(
            "UnitHelicopters",
            """
            [
              {
                "type":"TEST_HELICOPTER",
                "Operators":{},
                "countriesWorldID":[],
                "displayName":"Test Helicopter",
                "module":"",
                "shape":"test-helicopter-shape",
                "detectionRange":0,
                "threatRangeMin":0,
                "threatRange":0,
                "paintSchemes":{},
                "payloadPresets":[],
                "tasks":[],
                "fuel":1000,
                "flares":0,
                "chaff":0,
                "radio":{"frequency":251.0,"modulation":0},
                "maxAlt":10000,
                "cruiseSpeed":200,
                "panelRadio":[],
                "extraProps":[],
                "EPLRS":false,
                "ammoType":null,
                "height":1,
                "width":1,
                "length":1,
                "callsigns":{}
              }
            ]
            """,
            """
            [
              {
                "type":"TEST_HELICOPTER",
                "families":["HelicopterAttack"],
                "lowPolly":false,
                "immovable":false,
                "playerControllable":true
              }
            ]
            """);

        var messages = CaptureWarnings(() => DBEntryAircraft.LoadJSON(fixture.Db, tempFiles.DataFilePath, fixture.Db.Language));
        Assert.Contains(messages, x => x.Contains("\"TEST_HELICOPTER\" operational data is empty.", StringComparison.Ordinal));
    }

    [Fact]
    public void CarParsingWarnsWhenOperationalDataIsEmpty()
    {
        using var tempFiles = new JsonTestFiles(
            "UnitCars",
            """
            [
              {
                "type":"TEST_CAR",
                "Operators":{},
                "countriesWorldID":[],
                "displayName":"Test Car",
                "module":"",
                "shape":"test-car-shape",
                "category":"Vehicle",
                "detectionRange":0,
                "threatRangeMin":0,
                "threatRange":0,
                "paintSchemes":{}
              }
            ]
            """,
            """
            [
              {
                "type":"TEST_CAR",
                "families":["VehicleAPC"],
                "lowPolly":false,
                "immovable":false,
                "playerControllable":false
              }
            ]
            """);

        var messages = CaptureWarnings(() => DBEntryCar.LoadJSON(tempFiles.DataFilePath, fixture.Db.Language));
        Assert.Contains(messages, x => x.Contains("\"TEST_CAR\" operational data is empty.", StringComparison.Ordinal));
    }

    [Fact]
    public void ShipParsingWarnsWhenOperationalDataIsEmpty()
    {
        using var tempFiles = new JsonTestFiles(
            "UnitShips",
            """
            [
              {
                "type":"TEST_SHIP",
                "Operators":{},
                "countriesWorldID":[],
                "displayName":"Test Ship",
                "module":"",
                "shape":"test-ship-shape",
                "detectionRange":0,
                "threatRangeMin":0,
                "threatRange":0,
                "categories":[],
                "helicopterStorage":0,
                "planeStorage":0
              }
            ]
            """,
            """
            [
              {
                "type":"TEST_SHIP",
                "families":["ShipFrigate"],
                "lowPolly":false,
                "immovable":false,
                "playerControllable":false
              }
            ]
            """);

        var messages = CaptureWarnings(() => DBEntryShip.LoadJSON(tempFiles.DataFilePath, fixture.Db.Language));
        Assert.Contains(messages, x => x.Contains("\"TEST_SHIP\" operational data is empty.", StringComparison.Ordinal));
    }

    private List<string> CaptureWarnings(Action action)
    {
        var messages = new List<string>();
        var briefRoom = new BriefingRoom(fixture.Db, (message, errorLevel) =>
        {
            if (errorLevel == LogMessageErrorLevel.Warning)
                messages.Add(message);
        });

        try
        {
            action();
        }
        finally
        {
            briefRoom.SetLogHandler(null);
        }

        return messages;
    }

    private sealed class JsonTestFiles : IDisposable
    {
        private readonly string directoryPath;

        public string DataFilePath { get; }

        public JsonTestFiles(string datasetName, string jsonContent, string brInfoContent)
        {
            directoryPath = Path.Combine(Path.GetTempPath(), $"br-operational-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directoryPath);

            DataFilePath = Path.Combine(directoryPath, $"{datasetName}.json");
            var brInfoPath = Path.Combine(directoryPath, $"{datasetName}BRInfo.json");

            File.WriteAllText(DataFilePath, jsonContent);
            File.WriteAllText(brInfoPath, brInfoContent);
        }

        public void Dispose()
        {
            if (Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);
        }
    }
}
