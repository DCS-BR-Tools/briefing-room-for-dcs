using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.Mission.Objectives;
using BriefingRoom4DCS.Mission;

namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class EscortRampAndWaypointTests
{
    private readonly DatabaseFixture fixture;

    public EscortRampAndWaypointTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Theory]
    [InlineData("ToFrontLineFromPlayerAirbase", true)]
    [InlineData("ToFrontLineFromAirbase", true)]
    [InlineData("ParkedOnAirBase", true)]
    [InlineData("ToFrontLine", false)]
    [InlineData("Patrolling", false)]
    public void IsRampSpawnLocationMatchesAirbaseAndPlayerAirbaseOrigins(string behaviorId, bool expected)
    {
        var behavior = fixture.Db.GetEntry<DBEntryObjectiveTargetBehavior>(behaviorId);
        Assert.NotNull(behavior);
        Assert.Equal(expected, Escort.IsRampSpawnLocation(behavior.Location));
    }

    [Fact]
    public void PlayerAirbaseRampEscortOmitsCruiseAltitudeAndKeepsPickupWaypoint()
    {
        var templatePath = Path.Combine(Path.GetTempPath(), $"escort-ramp-test-{Guid.NewGuid():N}.brt");
        try
        {
            File.WriteAllText(templatePath, """
                [context]
                coalitionblue=USA
                coalitionred=Russia
                playercoalition=Blue
                theater=Caucasus
                situation=0

                [playerflightgroups]
                playerflightgroup000.aircrafttype=F-16C_50
                playerflightgroup000.aiwingmen=False
                playerflightgroup000.hostile=False
                playerflightgroup000.count=2
                playerflightgroup000.payload=default
                playerflightgroup000.country=CombinedJointTaskForcesBlue
                playerflightgroup000.startlocation=ParkingHot
                playerflightgroup000.livery=default
                playerflightgroup000.overrideradioband=AM
                playerflightgroup000.overridecallsignnumber=1

                [missionfeatures]

                [mods]

                [options]
                mission=ImperialUnitsForBriefing

                [player]
                startairbase=Batumi

                [objectives]
                objective000.preset=Custom
                objective000.task=Escort
                objective000.target=PlaneAWACS
                objective000.targetbehavior=ToFrontLineFromPlayerAirbase
                objective000.coordinatehint=0,0
                """);

            var briefingRoom = new BriefingRoom(fixture.Db);
            var mission = briefingRoom.GenerateMission(templatePath);

            var tasks = mission.Briefing.GetItems(DCSMissionBriefingItemType.Task)
                .Select(x => x.ToLowerInvariant())
                .ToList();
            var flightGroups = mission.Briefing.GetItems(DCSMissionBriefingItemType.FlightGroup);
            var playerWaypoints = mission.Briefing.GetItems(DCSMissionBriefingItemType.Waypoint);

            Assert.Contains(flightGroups, x => x.Contains("(ESCORT)") && x.Contains("(Ramp)"));
            Assert.DoesNotContain(tasks, x => x.Contains("approximately") && x.Contains("ft"));
            Assert.Contains(playerWaypoints, x => x.Contains("P-") && x.Contains("Pickup"));
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }
}
