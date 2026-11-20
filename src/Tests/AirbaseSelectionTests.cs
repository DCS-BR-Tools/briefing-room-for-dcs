using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class AirbaseSelectionTests
{
    private readonly DatabaseFixture fixture;

    public AirbaseSelectionTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public void RandomDestinationAirbase_Helicopter_Respects_SeparationCap()
    {
        var briefingRoom = new BriefingRoom(fixture.Db);
        var template = new MissionTemplate(
            fixture.Db,
            $"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\Caucasus.brt");
        template.ContextSituation = "None";
        template.PlayerFlightGroups[0].Aircraft = "UH-1H";
        template.FlightPlanTheaterStartingAirbase = "CaucasusBatumi";
        template.FlightPlanTheaterDestinationAirbase = "";
        template.FlightPlanObjectiveDistanceMin = 10;
        template.FlightPlanObjectiveDistanceMax = 80;
        template.FlightPlanObjectiveSeparationMin = 10;
        template.FlightPlanObjectiveSeparationMax = 45;

        double maxAllowedMeters = 45 * 1.3 * Toolbox.NM_TO_METERS;
        var seenDestinations = new HashSet<string>();

        for (int i = 0; i < 10; i++)
        {
            template.RandomSeed = 700 + i;
            var mission = briefingRoom.GenerateMission(template);
            double distanceMeters = mission.PlayerAirbase.Coordinates.GetDistanceFrom(mission.PlayerAirbaseDestination.Coordinates);
            Assert.True(
                distanceMeters <= maxAllowedMeters,
                $"Destination {mission.PlayerAirbaseDestination.ID} was {distanceMeters * Toolbox.METERS_TO_NM:F1} NM away (max {45 * 1.3:F1} NM).");
            seenDestinations.Add(mission.PlayerAirbaseDestination.ID);
        }

        Assert.DoesNotContain("CaucasusBatumi", seenDestinations);
    }

    [Fact]
    public void RandomDestinationAirbase_Helicopter_Respects_ObjectiveDistanceCap_WhenLower()
    {
        var briefingRoom = new BriefingRoom(fixture.Db);
        var template = new MissionTemplate(
            fixture.Db,
            $"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\Caucasus.brt");
        template.ContextSituation = "None";
        template.PlayerFlightGroups[0].Aircraft = "UH-1H";
        template.FlightPlanTheaterStartingAirbase = "CaucasusBatumi";
        template.FlightPlanTheaterDestinationAirbase = "";
        template.FlightPlanObjectiveDistanceMin = 10;
        template.FlightPlanObjectiveDistanceMax = 45;
        template.FlightPlanObjectiveSeparationMin = 10;
        template.FlightPlanObjectiveSeparationMax = 100;

        double maxAllowedMeters = 45 * 1.3 * Toolbox.NM_TO_METERS;

        for (int i = 0; i < 10; i++)
        {
            template.RandomSeed = 720 + i;
            var mission = briefingRoom.GenerateMission(template);
            double distanceMeters = mission.PlayerAirbase.Coordinates.GetDistanceFrom(mission.PlayerAirbaseDestination.Coordinates);
            Assert.True(
                distanceMeters <= maxAllowedMeters,
                $"Destination {mission.PlayerAirbaseDestination.ID} was {distanceMeters * Toolbox.METERS_TO_NM:F1} NM away (max {45 * 1.3:F1} NM).");
        }
    }

    [Fact]
    public void RandomDestinationAirbase_FixedWing_Respects_ObjectiveDistanceCap()
    {
        var briefingRoom = new BriefingRoom(fixture.Db);
        var template = new MissionTemplate(
            fixture.Db,
            $"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\Caucasus.brt");
        template.ContextSituation = "None";
        template.PlayerFlightGroups[0].Aircraft = "F-16C_50";
        template.FlightPlanTheaterStartingAirbase = "CaucasusBatumi";
        template.FlightPlanTheaterDestinationAirbase = "";
        template.FlightPlanObjectiveDistanceMin = 20;
        template.FlightPlanObjectiveDistanceMax = 80;
        template.FlightPlanObjectiveSeparationMin = 10;
        template.FlightPlanObjectiveSeparationMax = 25;

        double maxAllowedMeters = 80 * 1.3 * Toolbox.NM_TO_METERS;

        for (int i = 0; i < 10; i++)
        {
            template.RandomSeed = 750 + i;
            var mission = briefingRoom.GenerateMission(template);
            double distanceMeters = mission.PlayerAirbase.Coordinates.GetDistanceFrom(mission.PlayerAirbaseDestination.Coordinates);
            Assert.True(
                distanceMeters <= maxAllowedMeters,
                $"Destination {mission.PlayerAirbaseDestination.ID} was {distanceMeters * Toolbox.METERS_TO_NM:F1} NM away (max {80 * 1.3:F1} NM).");
        }
    }

    [Fact]
    public void DestinationAirbase_Home_And_Explicit_Ids_Work_As_Expected()
    {
        var briefingRoom = new BriefingRoom(fixture.Db);
        var template = new MissionTemplate(
            fixture.Db,
            $"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\Caucasus.brt");
        template.RandomSeed = 780;
        template.PlayerFlightGroups[0].Aircraft = "UH-1H";
        template.FlightPlanTheaterStartingAirbase = "CaucasusBatumi";

        template.FlightPlanTheaterDestinationAirbase = "home";
        var homeMission = briefingRoom.GenerateMission(template);
        Assert.Equal("CaucasusBatumi", homeMission.PlayerAirbaseDestination.ID);

        template.FlightPlanTheaterDestinationAirbase = "CaucasusKobuleti";
        var explicitMission = briefingRoom.GenerateMission(template);
        Assert.Equal("CaucasusKobuleti", explicitMission.PlayerAirbaseDestination.ID);
    }

    [Fact]
    public void RandomDestinationAirbase_FallbackToStartingAirbase_DoesNotDuplicateRegistration()
    {
        var briefingRoom = new BriefingRoom(fixture.Db);
        var template = new MissionTemplate(
            fixture.Db,
            $"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\Caucasus.brt");
        template.RandomSeed = 700;
        template.ContextSituation = "None";
        template.PlayerFlightGroups[0].Aircraft = "UH-1H";
        template.FlightPlanTheaterStartingAirbase = "CaucasusBatumi";
        template.FlightPlanTheaterDestinationAirbase = "";
        template.FlightPlanObjectiveDistanceMin = 5;
        template.FlightPlanObjectiveDistanceMax = 10;
        template.FlightPlanObjectiveSeparationMin = 5;
        template.FlightPlanObjectiveSeparationMax = 10;

        var mission = briefingRoom.GenerateMission(template);

        Assert.Equal("CaucasusBatumi", mission.PlayerAirbase.ID);
        Assert.Equal("CaucasusBatumi", mission.PlayerAirbaseDestination.ID);
        Assert.Equal(
            1,
            mission.PopulatedAirbaseIds[mission.TemplateRecord.ContextPlayerCoalition].Count(id => id == mission.PlayerAirbase.DCSID));
        Assert.Single(mission.Briefing.GetItems(DCSMissionBriefingItemType.Airbase));
        Assert.Contains(mission.GetMapData().Keys, k => k.StartsWith("AIRBASE_HOME_NAME_"));
        Assert.DoesNotContain(mission.GetMapData().Keys, k => k.StartsWith("AIRBASE_DEST_NAME_"));
    }
}

