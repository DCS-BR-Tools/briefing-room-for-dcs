using System.IO;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.Mission.Objectives;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

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

            var flightGroups = mission.Briefing.GetItems(DCSMissionBriefingItemType.FlightGroup);
            Assert.Contains(tasks, x => x.Contains("approximately") && x.Contains("ft"));
            Assert.Contains(flightGroups, x => x.Contains("(ESCORT)"));
            Assert.DoesNotContain(tasks, x => x.Contains("ERR_Missing_Translation_key", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(flightGroups, x => x.Contains("ERR_Missing_Translation_key", StringComparison.OrdinalIgnoreCase));
            Assert.NotEmpty(remarks);
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }

    [Fact]
    public void EscortLanguageKeysExistInAllLanguages()
    {
        var language = fixture.Db.Language;
        var languageCodes = language.AvailableLanguagesMap.Keys.Select(x => x.ToLowerInvariant()).ToList();
        string[] keys =
        [
            "EscortAltitudeClause",
            "EscortAltitudeAGL",
            "EscortAltitudeMSL",
            "EscortDepartureRamp",
            "EscortDepartureAirSpawn",
            "EscortDepartureWithSpeed",
            "EscortFlightTag"
        ];

        foreach (var key in keys)
        {
            Assert.True(language.LangMap.ContainsKey(key.ToUpperInvariant()), $"Missing translation key \"{key}\"");
            var translations = language.LangMap[key.ToUpperInvariant()];
            foreach (var languageCode in languageCodes)
                Assert.True(translations.ContainsKey(languageCode), $"Translation key \"{key}\" missing for language \"{languageCode}\"");
        }
    }

    [Fact]
    public void EscortSpeedIgnoresDifferentCategoryPlayerAircraft()
    {
        var su25t = Assert.IsAssignableFrom<DBEntryAircraft>(fixture.Db.GetEntry<DBEntryJSONUnit>("Su-25T"));
        var ka50 = Assert.IsAssignableFrom<DBEntryAircraft>(fixture.Db.GetEntry<DBEntryJSONUnit>("Ka-50"));
        var f15c = Assert.IsAssignableFrom<DBEntryAircraft>(fixture.Db.GetEntry<DBEntryJSONUnit>("F-15C"));

        var playerGroups = new List<MissionTemplateFlightGroupRecord>
        {
            new(new MissionTemplateFlightGroup(fixture.Db) { Aircraft = "Su-25T" }, 0),
            new(new MissionTemplateFlightGroup(fixture.Db) { Aircraft = "Ka-50" }, 1)
        };

        var speed = Escort.GetEscortSpeed(fixture.Db, f15c, playerGroups);
        Assert.Equal(Math.Min(f15c.CruiseSpeed, su25t.CruiseSpeed), speed);
        Assert.NotEqual(ka50.CruiseSpeed, speed);
    }
}
