using System.IO;
using BriefingRoom4DCS.Mission;

namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class ObjectiveBriefingTests
{
    private readonly DatabaseFixture fixture;

    public ObjectiveBriefingTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public void EscortAndDynamicSpawnObjectivesAddSpawnDetailsToBriefing()
    {
        var templatePath = Path.Combine(Path.GetTempPath(), $"escort-briefing-{Guid.NewGuid():N}.brt");
        try
        {
            File.WriteAllText(templatePath, """
                [context]
                coalitionblue=USA
                coalitionred=Russia
                decade=Decade2000
                playercoalition=Blue
                theater=Caucasus

                [flightplan]
                objectivedistancemax=160
                objectivedistancemin=40
                objectiveseparationmax=100
                objectiveseparationmin=10
                borderlimit=100

                [missionfeatures]
                missionfeatures=FriendlyAWACS,FriendlyTankerBasket,FriendlyTankerBoom

                [mods]

                [options]
                fogofwar=All
                mission=ImperialUnitsForBriefing,MarkWaypoints,DisableKneeboardImages
                realism=DisableDCSRadioAssists,NoBDA

                [playerflightgroups]
                playerflightgroup000.aircrafttype=Su-25T
                playerflightgroup000.aiwingmen=False
                playerflightgroup000.hostile=False
                playerflightgroup000.count=2
                playerflightgroup000.payload=default
                playerflightgroup000.country=CombinedJointTaskForcesRed
                playerflightgroup000.startlocation=Runway
                playerflightgroup000.livery=default
                playerflightgroup000.overrideradioband=AM
                playerflightgroup000.overridecallsignnumber=1

                [situation]
                enemyskill=Random
                enemyairdefense=Random
                enemyairforce=Random
                friendlyskill=Random
                friendlyairdefense=Random
                friendlyairforce=Random

                [combinedarms]
                commanderblue=0
                commanderred=0
                jtacblue=0
                jtacred=0

                [briefing]

                [environment]
                season=Random
                timeofday=RandomDaytime
                wind=Random

                [objectives]
                objective000.preset=EscortPlane
                objective000.coordinatehint=0,0
                """);

            var briefingRoom = new BriefingRoom(fixture.Db);
            var mission = briefingRoom.GenerateMission(templatePath);

            var tasks = mission.Briefing.GetItems(DCSMissionBriefingItemType.Task)
                .Select(x => x.ToLowerInvariant())
                .ToList();

            var remarks = mission.Briefing.GetItems(DCSMissionBriefingItemType.Remark)
                .Select(x => x.ToLowerInvariant())
                .ToList();

            Assert.Contains(tasks, x => x.Contains("approximately") && x.Contains("ft"));
            Assert.NotEmpty(remarks);
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }


}
