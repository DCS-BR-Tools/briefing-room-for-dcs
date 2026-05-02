using System.Text.RegularExpressions;
using BriefingRoom4DCS.Mission;

namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class SmokeTests
{
    DatabaseFixture fixture;

    public SmokeTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Theory]
    [InlineData("Afghanistan")]
    [InlineData("Caucasus")]
    [InlineData("Falklands")]
    [InlineData("GermanyCW")]
    [InlineData("Iraq")]
    [InlineData("Kola")]
    [InlineData("MarianaIslands")]
    [InlineData("MarianaIslandsWWII")]
    [InlineData("Nevada")]
    [InlineData("Normandy")]
    [InlineData("PersianGulf")]
    [InlineData("SinaiMap")]
    [InlineData("Syria")]
    [InlineData("TheChannel")]
    public void SingleMission(string theaterID)
    {
        var briefingRoom = new BriefingRoom(fixture.Db);

        DCSMission mission = briefingRoom.GenerateMission($"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\{theaterID}.brt");

        Assert.NotNull(mission);
        Assert.Equal(theaterID, mission.TheaterID);
        Assert.NotNull(mission.Briefing.Name);
        Assert.NotNull(mission.Briefing.Description);
    }

    [Fact]
    public void Campaign()
    {
        var briefingRoom = new BriefingRoom(fixture.Db);

        DCSCampaign campaign = briefingRoom.GenerateCampaign($"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\Test.cbrt");

        Assert.NotNull(campaign);
        Assert.Equal(3, campaign.MissionCount);
        Assert.NotNull(campaign.Name);


        DCSMission mission = campaign.Missions.First();
        Assert.NotNull(mission);
        Assert.Equal("Caucasus", mission.TheaterID);
        Assert.NotNull(mission.Briefing.Name);
        Assert.NotNull(mission.Briefing.Description);
    }

    // Regression for #1071. HoldSuperiority adds HiddenEnemy*AttackingObj features whose aircraft groups
    // get TimedAircraftActivation, scheduled by AircraftActivator.lua off the group name "-TQ-{N}-".
    // When the parent task is gated on Progression, the default 1-60 minute random delay can make the
    // objective unwinnable (player loiters in zone, nothing spawns). The fix bounds N <= 5 in that case.
    [Fact]
    public void HoldSuperiorityWithProgressionBoundsAttackerDelay()
    {
        var briefingRoom = new BriefingRoom(fixture.Db);

        DCSMission mission = briefingRoom.GenerateMission(
            $"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\HoldSuperiorityProgression.brt");

        Assert.NotNull(mission);
        Assert.Equal("Caucasus", mission.TheaterID);

        var unitsLua =
            mission.GetValue("CountriesBlue") +
            mission.GetValue("CountriesRed") +
            mission.GetValue("CountriesNeutral");

        var tqMatches = Regex.Matches(unitsLua, @"-TQ-(\d+)-");
        // The bounded delay only behaves as intended if AircraftActivator.lua can match the TQ group
        // back to its objective: the activator does `string.match(name, objective.name)` and only then
        // applies `actTime + objective.startMinutes`. UnitGenerator appends `-{ObjectiveName}` to TQ
        // names, so every TQ group should carry that suffix (uppercase WP-objective callsign).
        var linkedMatches = Regex.Matches(unitsLua, @"-TQ-(\d+)--[A-Z]+");
        Assert.Equal(tqMatches.Count, linkedMatches.Count);

        // Not asserting that any TQ groups exist (HiddenEnemy features are probabilistic via RollChance),
        // but every one we do find must be inside the bounded window when the parent task is progression-gated.
        foreach (Match m in tqMatches)
        {
            var minutes = int.Parse(m.Groups[1].Value);
            Assert.InRange(minutes, 1, 5);
        }
    }
}